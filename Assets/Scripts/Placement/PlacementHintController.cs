using System;
using BallGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Placement
{
    [DisallowMultipleComponent]
    public sealed class PlacementHintController : MonoBehaviour
    {
        public enum HintState { Idle, WaitingForAd, Assisting }
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlacementManager placementManager;
        [SerializeField] private CameraController2D cameraController;
        [SerializeField] private HintRewardedAdProvider adProvider;
        [SerializeField] private Button hintButton;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField, Min(1f)] private float adTimeout = 120f;

        private PlacementHintGroup activeGroup;
        private PlacementHintTarget activeTarget;
        private bool needsReevaluation;
        private bool awaitingBuildRestore;
        private bool refocusAfterRestore;
        private string configurationError;
        public PlacementHintGroup ActiveGroup => activeGroup;
        private PlacementHintGhost ghost;
        private int requestVersion;
        private float requestStartedAt;
        private string feedback;
        private float feedbackUntil;
        private bool ownsInputBlock;
        public HintState State { get; private set; }
        public PlacementHintTarget ActiveTarget => activeTarget;

        public void Bind(GameManager manager, PlacementManager placement, CameraController2D camera,
            Button button, TMP_Text label)
        {
            if (gameManager == manager && placementManager == placement && cameraController == camera
                && hintButton == button && hintLabel == label)
            {
                EnsureAdProvider();
                RefreshButton();
                return;
            }

            bool wasActive = isActiveAndEnabled;
            if (wasActive) OnDisable();
            gameManager = manager;
            placementManager = placement;
            cameraController = camera;
            hintButton = button;
            hintLabel = label;
            EnsureAdProvider();
            if (wasActive) OnEnable();
        }

        private void EnsureAdProvider()
        {
            if (adProvider != null) return;
            adProvider = GetComponent<HintRewardedAdProvider>();
            if (adProvider != null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            adProvider = gameObject.AddComponent<MockHintAdProvider>();
#else
            adProvider = gameObject.AddComponent<LegacyHintAdAdapter>();
#endif
        }

        private void Awake()
        {
            if (gameManager == null) gameManager = GetComponent<GameManager>();
            if (placementManager == null) placementManager = GetComponent<PlacementManager>();
            if (cameraController == null) cameraController = FindObjectOfType<CameraController2D>();
        }
        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.BuildRestored -= OnBuildRestored;
                gameManager.LevelResetting -= ClearHint;
                gameManager.StateChanged -= OnGameStateChanged;
                gameManager.BuildRestored += OnBuildRestored;
                gameManager.LevelResetting += ClearHint;
                gameManager.StateChanged += OnGameStateChanged;
            }
            if (placementManager != null)
            {
                placementManager.HistoryChanged -= MarkDirty;
                placementManager.InventoryChanged -= MarkDirty;
                placementManager.LevelResetting -= ClearHint;
                placementManager.HistoryChanged += MarkDirty;
                placementManager.InventoryChanged += MarkDirty;
                placementManager.LevelResetting += ClearHint;
                placementManager.HintController = this;
            }
            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(RequestHint);
                hintButton.onClick.AddListener(RequestHint);
            }
            RefreshButton();
        }
        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.BuildRestored -= OnBuildRestored;
                gameManager.LevelResetting -= ClearHint;
                gameManager.StateChanged -= OnGameStateChanged;
            }
            if (placementManager != null)
            {
                placementManager.HistoryChanged -= MarkDirty;
                placementManager.InventoryChanged -= MarkDirty;
                placementManager.LevelResetting -= ClearHint;
                if (placementManager.HintController == this) placementManager.HintController = null;
            }
            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(RequestHint);
                hintButton.interactable = false;
            }
            ClearHint();
        }
        private bool IsBuild => gameManager != null && gameManager.State == GameState.Build;
        private bool DependenciesReady => gameManager != null && placementManager != null
            && cameraController != null && cameraController.isActiveAndEnabled;

        private void Update()
        {
            if (feedback != null && Time.unscaledTime >= feedbackUntil)
            {
                feedback = null;
                gameManager?.ShowPlacementHintFeedback(null);
            }
            if (State == HintState.WaitingForAd && Time.unscaledTime - requestStartedAt >= adTimeout)
            {
                ClearHint();
                ShowFeedback("广告超时，请重试");
            }
            if (State == HintState.Assisting)
            {
                if (activeGroup == null || !DependenciesReady)
                {
                    ClearHint();
                    ShowFeedback("提示配置不可用");
                }
                else
                {
                    if (ownsInputBlock && !cameraController.IsHintFocusMoving) ReleaseInput();
                    if (needsReevaluation) EvaluateActiveGroup();
                    if (ghost != null)
                    {
                        ghost.gameObject.SetActive(IsBuild && !awaitingBuildRestore);
                        ghost.SetHighlighted(IsBuild && !ownsInputBlock && activeTarget.IsInRange(placementManager.DraggingTool));
                    }
                }
            }
            RefreshButton();
        }

        private PlacementHintGroup FindNextGroup(out string error)
        {
            error = null;
            if (gameManager == null || placementManager == null || gameManager.CurrentLevel == null) return null;
            foreach (var group in gameManager.CurrentLevel.GetEffectivePlacementHintGroups())
            {
                if (group == null) { error = "提示组配置不可用"; return null; }
                if (!group.Validate(out error)) return null;
                var matches = group.MatchTargets(placementManager.PlacedTools);
                if (Array.IndexOf(matches, -1) < 0) continue;
                if (!group.HasEnoughTools(placementManager, out error)) return null;
                return group;
            }
            return null;
        }

        // Retained for callers that only need the next visible target.
        public PlacementHintTarget FindNextTarget()
        {
            var group = FindNextGroup(out _);
            if (group == null) return null;
            int index = Array.IndexOf(group.MatchTargets(placementManager.PlacedTools), -1);
            return index >= 0 ? group.targets[index] : null;
        }

        private void MarkDirty() => needsReevaluation = true;

        private void OnBuildRestored()
        {
            awaitingBuildRestore = false;
            refocusAfterRestore = true;
            needsReevaluation = true;
            EvaluateActiveGroup();
        }

        private void EvaluateActiveGroup()
        {
            if (State != HintState.Assisting || !IsBuild || awaitingBuildRestore || !DependenciesReady
                || placementManager.DraggingTool != null || cameraController.IsHintFocusMoving) return;
            needsReevaluation = false;
            if (activeGroup == null || !activeGroup.Validate(out _))
            {
                ClearHint();
                ShowFeedback("提示组配置不可用");
                return;
            }
            var matches = activeGroup.MatchTargets(placementManager.PlacedTools);
            int firstMissing = Array.IndexOf(matches, -1);
            if (firstMissing < 0)
            {
                ClearHint();
                ShowFeedback("本组辅助放置完成");
                return;
            }
            int current = activeGroup.targets.IndexOf(activeTarget);
            var next = current >= 0 && matches[current] < 0 ? activeTarget : activeGroup.targets[firstMissing];
            bool changed = next != activeTarget || ghost == null;
            if (changed)
            {
                DestroyGhost();
                activeTarget = next;
                ghost = PlacementHintGhost.Create(next, hintLabel != null ? hintLabel.font : null);
            }
            if (changed || refocusAfterRestore)
            {
                refocusAfterRestore = false;
                BlockInput();
                cameraController.BeginHintFocus(next.cameraPosition, next.cameraMoveDuration);
            }
        }

        private void DestroyGhost()
        {
            if (ghost == null) return;
            ghost.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(ghost.gameObject);
            else DestroyImmediate(ghost.gameObject);
            ghost = null;
        }

        public void RequestHint()
        {
            if (!isActiveAndEnabled || State != HintState.Idle || !IsBuild || !DependenciesReady
                || !placementManager.IsBuildInteractionAvailable || placementManager.DraggingTool != null) return;
            var group = FindNextGroup(out string error);
            if (group == null) { ShowFeedback(error ?? "无需提示"); return; }
            if (adProvider == null || !adProvider.IsAvailable) { ShowFeedback("广告暂不可用"); return; }
            activeGroup = group;
            State = HintState.WaitingForAd;
            int version = ++requestVersion;
            var level = gameManager.CurrentLevel;
            requestStartedAt = Time.unscaledTime;
            feedback = null;
            gameManager.ShowPlacementHintFeedback(null);
            BlockInput();
            RefreshButton();
            try
            {
                adProvider.Show(result =>
                {
                    if (this == null || !isActiveAndEnabled || version != requestVersion
                        || State != HintState.WaitingForAd || gameManager == null || gameManager.CurrentLevel != level) return;
                    // Invalidate before doing any work: duplicated SDK callbacks must not regrant.
                    ++requestVersion;
                    if (result != HintAdResult.Rewarded || !IsBuild || !DependenciesReady || !group.Validate(out _) || !group.HasEnoughTools(placementManager, out _))
                    {
                        ClearHint();
                        ShowFeedback(result == HintAdResult.Cancelled ? "广告已取消" : "广告未完成，请重试");
                        return;
                    }
                    State = HintState.Assisting;
                    needsReevaluation = true;
                    EvaluateActiveGroup();
                    RefreshButton();
                });
            }
            catch (Exception exception)
            {
                ClearHint();
                ShowFeedback("广告暂不可用");
                Debug.LogException(exception, this);
            }
        }

        // Called only on pointer release before the existing collision/commit path.
        public bool TrySnap(PlaceableTool candidate)
        {
            if (State != HintState.Assisting || !IsBuild || ownsInputBlock || activeTarget == null
                || !activeTarget.IsValid || !activeTarget.IsInRange(candidate)) return false;
            candidate.transform.SetPositionAndRotation(activeTarget.position, activeTarget.Rotation);
            return true;
        }
        public void NotifyPlacementCommitted(PlaceableTool candidate)
        {
            if (State != HintState.Assisting || candidate == null) return;
            needsReevaluation = true;
            // PlacementManager calls this only after clearing transient drag references/history.
            EvaluateActiveGroup();
        }
        public void ClearHint()
        {
            ++requestVersion;
            bool hadAd = State == HintState.WaitingForAd;
            State = HintState.Idle;
            activeTarget = null;
            DestroyGhost();
            activeGroup = null;
            needsReevaluation = false;
            awaitingBuildRestore = false;
            refocusAfterRestore = false;
            ReleaseInput();
            if (hadAd && adProvider != null) adProvider.CancelPending();
            if (feedback != null) gameManager?.ShowPlacementHintFeedback(null);
            feedback = null;
        }
        private void BlockInput()
        {
            ownsInputBlock = true;
            placementManager.SetHintInputBlocked(true);
            cameraController.SetHintInputBlocked(true);
        }
        private void ReleaseInput()
        {
            if (!ownsInputBlock) return;
            ownsInputBlock = false;
            if (placementManager != null) placementManager.SetHintInputBlocked(false);
            if (cameraController != null) cameraController.CancelHintFocus();
        }
        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Intro || state == GameState.Success) ClearHint();
            else if (state != GameState.Build)
            {
                if (State == HintState.WaitingForAd) ClearHint();
                else
                {
                    awaitingBuildRestore = true;
                    ReleaseInput();
                }
                if (ghost != null) ghost.gameObject.SetActive(false);
            }
            RefreshButton();
        }
        private void ShowFeedback(string message)
        {
            feedback = message;
            gameManager?.ShowPlacementHintFeedback(message);
            feedbackUntil = Time.unscaledTime + 2.5f;
            RefreshButton();
        }
        private void RefreshButton()
        {
            bool ready = IsBuild && DependenciesReady;
            string error = null;
            bool hasTarget = State == HintState.Idle && ready && FindNextGroup(out error) != null;
            if (State == HintState.Idle && ready && configurationError != error)
            {
                if (error != null || configurationError != null) gameManager.ShowPlacementHintFeedback(error);
                configurationError = error;
            }
            bool available = adProvider != null && adProvider.IsAvailable;
            if (hintButton != null) hintButton.interactable = isActiveAndEnabled && hasTarget
                && available && placementManager.IsBuildInteractionAvailable && placementManager.DraggingTool == null;
            // Help caption belongs to the HUD; only its interactability changes here.

        }
    }
}
