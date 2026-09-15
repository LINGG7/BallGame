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

        private PlacementHintTarget activeTarget;
        private PlacementHintGhost ghost;
        private PlaceableTool snappedCandidate;
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
                gameManager.LevelResetting -= ClearHint;
                gameManager.StateChanged -= OnGameStateChanged;
                gameManager.LevelResetting += ClearHint;
                gameManager.StateChanged += OnGameStateChanged;
            }
            if (placementManager != null)
            {
                placementManager.LevelResetting -= ClearHint;
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
                gameManager.LevelResetting -= ClearHint;
                gameManager.StateChanged -= OnGameStateChanged;
            }
            if (placementManager != null)
            {
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
                if (activeTarget == null || !activeTarget.IsValid || !DependenciesReady)
                {
                    ClearHint();
                    ShowFeedback("提示配置不可用");
                }
                else
                {
                    if (ownsInputBlock && !cameraController.IsHintFocusMoving) ReleaseInput();
                    if (ghost != null)
                    {
                        ghost.gameObject.SetActive(IsBuild);
                        ghost.SetHighlighted(IsBuild && !ownsInputBlock && activeTarget.IsInRange(placementManager.DraggingTool));
                    }
                }
            }
            RefreshButton();
        }

        // Ordered targets use the current committed placement, not persistent hint-consumption flags.
        public PlacementHintTarget FindNextTarget()
        {
            if (gameManager == null || placementManager == null || gameManager.CurrentLevel == null) return null;
            foreach (var target in gameManager.CurrentLevel.PlacementHints)
            {
                if (target == null || !target.IsValid) continue;
                bool satisfied = false;
                bool movableMatchingTool = false;
                foreach (var placed in placementManager.PlacedTools)
                {
                    if (target.IsSatisfiedBy(placed)) { satisfied = true; break; }
                    if (placed != null && placed.CanEdit && placed.Definition == target.tool) movableMatchingTool = true;
                }
                if (satisfied) continue;
                if (!placementManager.CanPlace(target.tool) && !movableMatchingTool) continue;
                // A visual-only ghost requires a visible sprite in the configured prefab.
                foreach (var sprite in target.tool.Prefab.GetComponentsInChildren<SpriteRenderer>())
                    if (sprite.enabled && sprite.sprite != null) return target;
            }
            return null;
        }

        public void RequestHint()
        {
            if (!isActiveAndEnabled || State != HintState.Idle || !IsBuild || !DependenciesReady
                || !placementManager.IsBuildInteractionAvailable || placementManager.DraggingTool != null) return;
            var target = FindNextTarget();
            if (target == null) { ShowFeedback("无需提示或配置不可用"); return; }
            if (adProvider == null || !adProvider.IsAvailable) { ShowFeedback("广告暂不可用"); return; }
            activeTarget = target;
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
                    if (result != HintAdResult.Rewarded || !IsBuild || !DependenciesReady || !target.IsValid)
                    {
                        ClearHint();
                        ShowFeedback(result == HintAdResult.Cancelled ? "广告已取消" : "广告未完成，请重试");
                        return;
                    }
                    State = HintState.Assisting;
                    ghost = PlacementHintGhost.Create(target, hintLabel != null ? hintLabel.font : null);
                    cameraController.BeginHintFocus(target.cameraPosition, target.cameraMoveDuration);
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
            snappedCandidate = null;
            if (State != HintState.Assisting || !IsBuild || ownsInputBlock || activeTarget == null
                || !activeTarget.IsValid || !activeTarget.IsInRange(candidate)) return false;
            candidate.transform.SetPositionAndRotation(activeTarget.position, activeTarget.Rotation);
            snappedCandidate = candidate;
            return true;
        }
        public void NotifyPlacementCommitted(PlaceableTool candidate)
        {
            if (State != HintState.Assisting || candidate == null || candidate != snappedCandidate
                || activeTarget == null || !activeTarget.IsSatisfiedBy(candidate)) return;
            ClearHint();
            ShowFeedback("辅助放置完成");
        }
        public void ClearHint()
        {
            ++requestVersion;
            bool hadAd = State == HintState.WaitingForAd;
            State = HintState.Idle;
            activeTarget = null;
            snappedCandidate = null;
            if (ghost != null)
            {
                ghost.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(ghost.gameObject);
                else DestroyImmediate(ghost.gameObject);
                ghost = null;
            }
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
                else ReleaseInput();
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
            bool hasTarget = State == HintState.Idle && ready && FindNextTarget() != null;
            bool available = adProvider != null && adProvider.IsAvailable;
            if (hintButton != null) hintButton.interactable = isActiveAndEnabled && hasTarget
                && available && placementManager.IsBuildInteractionAvailable && placementManager.DraggingTool == null;
            // Help caption belongs to the HUD; only its interactability changes here.

        }
    }
}
