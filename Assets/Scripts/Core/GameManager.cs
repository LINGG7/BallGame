using System;
using System.Collections.Generic;
using BallGame.Audio;
using BallGame.Placement;
using BallGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BallGame.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        private const string FailureResourcePath = "Audio/SFX/Failure";

        [Header("Scene References")]
        [SerializeField] private BallController ball;
        [SerializeField] private GoalTrigger goal;
        [SerializeField] private CameraController2D cameraController;
        [SerializeField] private PlacementManager placementManager;
        [SerializeField] private ToolInventoryPanel inventoryPanel;
        [SerializeField] private GameHudRefs hudRefs;

        [Header("UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button runButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button editButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button returnButton;

        [Header("Level")]
        [SerializeField] private LevelDefinition level;
        [SerializeField] private string nextSceneName;
        [SerializeField] private float failY = -8f;
        [SerializeField] private float simulationTimeout = 25f;
        [SerializeField, Min(0f)] private float failureResetDelay = 2f;
        [SerializeField] private int requiredGoalBallCount = 1;
        [SerializeField] private bool allowBallCollision = true;

        private float simulationStartedAt;
        private float simulationTimeRemaining;
        private float failureResetAtUnscaledTime = -1f;
        private bool completionRaised;
        private AudioClip failureSfx;
        private readonly List<BallController> activeBalls = new List<BallController>();
        private readonly HashSet<BallController> ballsInAnyGoal = new HashSet<BallController>();
        private readonly Dictionary<GoalTrigger, HashSet<BallController>> ballsByGoal = new Dictionary<GoalTrigger, HashSet<BallController>>();
        private readonly List<GoalTrigger> activeGoals = new List<GoalTrigger>();
        private readonly LevelCompletionResult currentCompletionResult = new LevelCompletionResult();

        public GameState State { get; private set; } = GameState.Intro;
        public bool IsSimulating => State == GameState.Simulate;
        public float SimulationTimeRemaining => simulationTimeRemaining;
        public bool HasNextLevel => !string.IsNullOrWhiteSpace(nextSceneName);
        public LevelCompletionResult CurrentCompletionResult => currentCompletionResult;
        public event Action<GameState> StateChanged;
        public event Action LevelResetting;
        public event Action BuildRestored;
        public LevelDefinition CurrentLevel => level;

        private void Awake()
        {
            failureSfx = Resources.Load<AudioClip>(FailureResourcePath);
            if (cameraController == null)
            {
                cameraController = FindObjectOfType<CameraController2D>();
            }

            if (hudRefs == null)
            {
                hudRefs = FindObjectOfType<GameHudRefs>();
            }

            ResolveHudReferences();

            if (hudRefs != null && hudRefs.PlacementControls != null)
            {
                hudRefs.PlacementControls.BindPlacementManager(placementManager);
            }

            if (placementManager != null)
            {
                placementManager.CostChanged += HandleCostChanged;
            }

            RefreshActiveGoals();
        }

        private void ResolveHudReferences()
        {
            if (hudRefs == null)
            {
                return;
            }

            if (inventoryPanel == null) inventoryPanel = hudRefs.InventoryPanel;
            if (titleText == null) titleText = hudRefs.TitleText;
            if (statusText == null) statusText = hudRefs.StatusText;
            if (runButton == null) runButton = hudRefs.RunButton;
            if (resetButton == null) resetButton = hudRefs.ResetButton;
            if (editButton == null) editButton = hudRefs.EditButton;
            if (nextButton == null) nextButton = hudRefs.NextButton;
            if (returnButton == null) returnButton = hudRefs.ReturnButton;
        }

        public void ShowPlacementHintFeedback(string message)
        {
            if (State == GameState.Build)
                SetStatus(message ?? "拖动道具，移动旋转，开始模拟。");
        }

        private void BindPlacementHints()
        {
            if (hudRefs == null) hudRefs = FindObjectOfType<GameHudRefs>();
            if (placementManager == null) placementManager = GetComponent<PlacementManager>();
            if (hudRefs == null || hudRefs.HelpButton == null || placementManager == null) return;

            var hints = GetComponent<PlacementHintController>();
            if (hints == null) hints = gameObject.AddComponent<PlacementHintController>();
            hints.Bind(this, placementManager, cameraController, hudRefs.HelpButton,
                hudRefs.HelpButton.GetComponentInChildren<TMP_Text>(true));
        }

        private void OnDestroy()
        {
            if (placementManager != null)
            {
                placementManager.CostChanged -= HandleCostChanged;
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(LoadMainMenu);
            }
        }

        private void Start()
        {
            BindPlacementHints();
            if (runButton != null)
            {
                runButton.onClick.AddListener(StartSimulation);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetToBuild);
            }

            if (editButton != null)
            {
                editButton.onClick.AddListener(ResetToBuild);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(LoadNextLevel);
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(LoadMainMenu);
                returnButton.onClick.AddListener(LoadMainMenu);
            }

            LoadCurrentLevel();
        }

        private void Update()
        {
            if (State == GameState.Failed)
            {
                if (failureResetAtUnscaledTime >= 0f && Time.unscaledTime >= failureResetAtUnscaledTime)
                {
                    ResetToBuild();
                }

                return;
            }

            if (State != GameState.Simulate)
            {
                return;
            }

            simulationTimeRemaining = Mathf.Max(0f, simulationTimeout - (Time.time - simulationStartedAt));
            UpdateSimulationStatus();
            if (HasAnyBallFailed() || simulationTimeRemaining <= 0f)
            {
                FailLevel();
            }
        }

        public void LoadCurrentLevel()
        {
            LevelResetting?.Invoke();
            CancelPendingFailureReset();
            if (level == null)
            {
                SetStatus("No level assigned.");
                placementManager?.SetInteractionEnabled(true);
                ChangeState(GameState.Build, true);
                return;
            }

            if (placementManager != null)
            {
                placementManager.ResetForLevel(level);
            }

            if (inventoryPanel != null)
            {
                inventoryPanel.Build(level, placementManager);
            }

            simulationTimeRemaining = simulationTimeout;
            completionRaised = false;
            currentCompletionResult.Reset();
            RefreshActiveGoals();
            ResetGoalTracking();
            RefreshActiveBalls();
            ApplyBallCollisionSettings(activeBalls);
            SyncTargetIndicators();
            BeginLevelIntro();
        }

        private void BeginLevelIntro()
        {
            ChangeState(GameState.Intro, true);
            placementManager?.SetInteractionEnabled(false);
            RefreshUi();

            LevelIntroController intro = hudRefs != null ? hudRefs.LevelIntro : FindObjectOfType<LevelIntroController>();
            if (intro == null)
            {
                FinishLevelIntro();
                return;
            }

            try
            {
                intro.Play(level != null ? level.LevelTitle : "Ball Puzzle", FinishLevelIntro);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, intro);
                FinishLevelIntro();
            }
        }

        private void FinishLevelIntro()
        {
            if (this == null || State != GameState.Intro)
            {
                return;
            }

            placementManager?.SetInteractionEnabled(true);
            ChangeState(GameState.Build, true);
            RefreshUi();
        }

        public void StartSimulation()
        {
            RefreshActiveBalls();
            if (State != GameState.Build || activeBalls.Count == 0)
            {
                return;
            }

            CancelPendingFailureReset();
            ChangeState(GameState.Simulate);
            simulationStartedAt = Time.time;
            simulationTimeRemaining = simulationTimeout;
            completionRaised = false;
            currentCompletionResult.Reset();
            RefreshActiveGoals();
            ResetGoalTracking();
            ApplyBallCollisionSettings(activeBalls);
            SyncTargetIndicators();
            cameraController?.SetSimulationTargets(GetBallTransforms(activeBalls));

            placementManager?.SetSimulationMode(true);
            for (int i = 0; i < activeBalls.Count; i++)
            {
                activeBalls[i].Launch();
            }

            RefreshUi();
        }

        public void ResetToBuild()
        {
            CancelPendingFailureReset();
            if (level == null || State == GameState.Intro)
            {
                return;
            }

            ChangeState(GameState.Build);
            simulationTimeRemaining = simulationTimeout;
            completionRaised = false;
            currentCompletionResult.Reset();
            RefreshActiveGoals();
            ResetGoalTracking();
            RefreshActiveBalls();
            ApplyBallCollisionSettings(activeBalls);
            SyncTargetIndicators();
            for (int i = 0; i < activeBalls.Count; i++)
            {
                activeBalls[i].ResetBall();
            }

            placementManager?.SetSimulationMode(false);
            cameraController?.ClearSimulationTargets();
            cameraController?.FocusOnBall(false);
            RefreshUi();
            BuildRestored?.Invoke();
        }

        public void RetryLevelAndClearTools()
        {
            LevelResetting?.Invoke();
            ResetToBuild();
            placementManager?.ClearAllPlacedTools();
        }

        public void RegisterBallInGoal(GoalTrigger targetGoal, BallController goalBall)
        {
            if (targetGoal == null || goalBall == null || State != GameState.Simulate || completionRaised)
            {
                return;
            }

            if (!ballsByGoal.TryGetValue(targetGoal, out HashSet<BallController> goalBalls))
            {
                goalBalls = new HashSet<BallController>();
                ballsByGoal[targetGoal] = goalBalls;
            }

            goalBalls.Add(goalBall);
            bool added = ballsInAnyGoal.Add(goalBall);
            if (added)
            {
                goalBall.StopAtGoal();
            }

            if (added && ballsInAnyGoal.Count >= Mathf.Max(1, requiredGoalBallCount))
            {
                CompleteLevel();
            }
        }

        public void UnregisterBallFromGoal(GoalTrigger targetGoal, BallController goalBall)
        {
            if (targetGoal == null || goalBall == null)
            {
                return;
            }

            if (ballsByGoal.TryGetValue(targetGoal, out HashSet<BallController> goalBalls))
            {
                goalBalls.Remove(goalBall);
                if (goalBalls.Count == 0)
                {
                    ballsByGoal.Remove(targetGoal);
                }
            }

            if (!IsBallInAnyTrackedGoal(goalBall))
            {
                ballsInAnyGoal.Remove(goalBall);
            }
        }

        public void CompleteLevel()
        {
            if (State != GameState.Simulate || completionRaised)
            {
                return;
            }

            completionRaised = true;
            CancelPendingFailureReset();
            EvaluateCompletionResult();
            EvaluateLowestCostResult();
            MarkCurrentLevelCompleted();
            ChangeState(GameState.Success);
            ResetGoalTracking();
            SetAllBallsBuildMode();
            placementManager?.SetSimulationMode(true);
            RefreshUi();
        }

        public void FailLevel()
        {
            if (!CanEnterFailure(State))
            {
                return;
            }

            ChangeState(GameState.Failed);
            PlayFailureSound();
            failureResetAtUnscaledTime = Time.unscaledTime + failureResetDelay;
            ResetGoalTracking();
            SetAllBallsBuildMode();
            RefreshUi();
        }

        private static bool CanEnterFailure(GameState state)
        {
            return state == GameState.Simulate;
        }

        private void PlayFailureSound()
        {
            AudioManager.Instance?.PlaySfx(failureSfx);
        }

        private void OnValidate()
        {
            failureResetDelay = Mathf.Max(0f, failureResetDelay);
            requiredGoalBallCount = Mathf.Max(1, requiredGoalBallCount);
        }

        private void CancelPendingFailureReset()
        {
            failureResetAtUnscaledTime = -1f;
        }

        private void ChangeState(GameState state, bool forceNotify = false)
        {
            if (!forceNotify && State == state)
            {
                return;
            }

            State = state;
            if (State != GameState.Simulate)
            {
                MobileHaptics.Stop();
            }

            if (hudRefs == null)
            {
                hudRefs = FindObjectOfType<GameHudRefs>();
            }

            hudRefs?.ApplyGameState(State);
            StateChanged?.Invoke(State);
        }

        private void RefreshActiveGoals()
        {
            activeGoals.Clear();
            GoalTrigger[] foundGoals = FindObjectsOfType<GoalTrigger>();
            for (int i = 0; i < foundGoals.Length; i++)
            {
                if (foundGoals[i] != null && foundGoals[i].isActiveAndEnabled)
                {
                    foundGoals[i].Initialize(this);
                    activeGoals.Add(foundGoals[i]);
                }
            }

            if (activeGoals.Count == 0 && goal != null)
            {
                goal.Initialize(this);
                activeGoals.Add(goal);
            }
        }

        private void ResetGoalTracking()
        {
            ballsInAnyGoal.Clear();
            ballsByGoal.Clear();
            for (int i = 0; i < activeGoals.Count; i++)
            {
                if (activeGoals[i] != null)
                {
                    activeGoals[i].ResetTrackedBalls();
                }
            }
        }

        private bool IsBallInAnyTrackedGoal(BallController goalBall)
        {
            foreach (KeyValuePair<GoalTrigger, HashSet<BallController>> entry in ballsByGoal)
            {
                if (entry.Value != null && entry.Value.Contains(goalBall))
                {
                    return true;
                }
            }

            return false;
        }

        private void EvaluateCompletionResult()
        {
            currentCompletionResult.Reset();
            LevelEndingRule[] endingRules = FindObjectsOfType<LevelEndingRule>();
            if (endingRules == null || endingRules.Length == 0)
            {
                return;
            }

            LevelCompletionContext context = CreateCompletionContext();
            for (int i = 0; i < endingRules.Length; i++)
            {
                if (endingRules[i] != null && endingRules[i].isActiveAndEnabled)
                {
                    endingRules[i].Evaluate(context, currentCompletionResult);
                }
            }
        }

        private void EvaluateLowestCostResult()
        {
            if (level == null || level.Mode != LevelMode.LowestCost)
            {
                return;
            }

            int currentCost = placementManager != null ? placementManager.CurrentCost : 0;
            string sceneName = SceneManager.GetActiveScene().name;
            bool isNewBest = LowestCostProgress.TryRecordBestCost(sceneName, currentCost, out int bestCost);
            currentCompletionResult.HasCostResult = true;
            currentCompletionResult.CurrentCost = currentCost;
            currentCompletionResult.BestCost = bestCost;
            currentCompletionResult.IsNewBestCost = isNewBest;
        }

        private LevelCompletionContext CreateCompletionContext()
        {
            Dictionary<GoalTrigger, int> countsByGoal = new Dictionary<GoalTrigger, int>();
            for (int i = 0; i < activeGoals.Count; i++)
            {
                GoalTrigger activeGoal = activeGoals[i];
                if (activeGoal == null)
                {
                    continue;
                }

                countsByGoal[activeGoal] = ballsByGoal.TryGetValue(activeGoal, out HashSet<BallController> goalBalls) ? goalBalls.Count : 0;
            }

            return new LevelCompletionContext(activeGoals.ToArray(), countsByGoal, ballsInAnyGoal.Count);
        }

        private void RefreshActiveBalls()
        {
            activeBalls.Clear();
            BallController[] foundBalls = FindObjectsOfType<BallController>();
            for (int i = 0; i < foundBalls.Length; i++)
            {
                if (foundBalls[i] != null && foundBalls[i].isActiveAndEnabled)
                {
                    activeBalls.Add(foundBalls[i]);
                }
            }

            if (activeBalls.Count == 0 && ball != null)
            {
                activeBalls.Add(ball);
            }
        }

        private bool HasAnyBallFailed()
        {
            RefreshActiveBalls();
            for (int i = 0; i < activeBalls.Count; i++)
            {
                BallController activeBall = activeBalls[i];
                if (activeBall != null && activeBall.transform.position.y < failY)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetAllBallsBuildMode()
        {
            RefreshActiveBalls();
            for (int i = 0; i < activeBalls.Count; i++)
            {
                activeBalls[i].SetBuildMode();
            }
        }

        private static Transform[] GetBallTransforms(List<BallController> balls)
        {
            Transform[] targets = new Transform[balls.Count];
            for (int i = 0; i < balls.Count; i++)
            {
                targets[i] = balls[i] != null ? balls[i].transform : null;
            }

            return targets;
        }

        private static Transform[] GetGoalTransforms(List<GoalTrigger> goals)
        {
            Transform[] targets = new Transform[goals.Count];
            for (int i = 0; i < goals.Count; i++)
            {
                targets[i] = goals[i] != null ? goals[i].transform : null;
            }

            return targets;
        }

        private void SyncTargetIndicators()
        {
            if (hudRefs == null)
            {
                hudRefs = FindObjectOfType<GameHudRefs>();
            }

            if (hudRefs == null)
            {
                return;
            }

            Camera worldCamera = cameraController != null ? cameraController.GetComponent<Camera>() : Camera.main;
            hudRefs.BindTargetIndicators(this, worldCamera, GetBallTransforms(activeBalls), GetGoalTransforms(activeGoals));
        }

        private void ApplyBallCollisionSettings(IReadOnlyList<BallController> balls)
        {
            bool ignoreCollision = !allowBallCollision;
            for (int i = 0; i < balls.Count; i++)
            {
                if (balls[i] == null)
                {
                    continue;
                }

                Collider2D[] firstColliders = balls[i].GetComponentsInChildren<Collider2D>();
                for (int j = i + 1; j < balls.Count; j++)
                {
                    if (balls[j] == null)
                    {
                        continue;
                    }

                    Collider2D[] secondColliders = balls[j].GetComponentsInChildren<Collider2D>();
                    SetCollisionIgnored(firstColliders, secondColliders, ignoreCollision);
                }
            }
        }

        private static void SetCollisionIgnored(Collider2D[] firstColliders, Collider2D[] secondColliders, bool ignored)
        {
            if (firstColliders == null || secondColliders == null)
            {
                return;
            }

            for (int i = 0; i < firstColliders.Length; i++)
            {
                Collider2D first = firstColliders[i];
                if (first == null)
                {
                    continue;
                }

                for (int j = 0; j < secondColliders.Length; j++)
                {
                    Collider2D second = secondColliders[j];
                    if (second != null)
                    {
                        Physics2D.IgnoreCollision(first, second, ignored);
                    }
                }
            }
        }

        public void LoadNextLevel()
        {
            if (HasNextLevel)
            {
                SceneTransitionController.LoadScene(nextSceneName);
            }
        }

        private static void LoadMainMenu()
        {
            SceneTransitionController.LoadScene("MainMenu");
        }

        private static void MarkCurrentLevelCompleted()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            const string challengePrefix = "Challenge_";
            if (sceneName.StartsWith(challengePrefix, StringComparison.Ordinal))
            {
                string challengeNumberText = sceneName.Substring(challengePrefix.Length);
                if (int.TryParse(challengeNumberText, out int challengeNumber))
                {
                    ChallengeProgress.MarkCompleted(challengeNumber);
                }

                return;
            }

            const string prefix = "Level_";
            if (!sceneName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return;
            }

            string levelNumberText = sceneName.Substring(prefix.Length);
            if (int.TryParse(levelNumberText, out int levelNumber))
            {
                LevelProgress.MarkCompleted(levelNumber);
            }
        }

        private void RefreshUi()
        {
            if (titleText != null)
            {
                titleText.text = level != null ? level.LevelTitle : "Ball Puzzle";
            }

            string message = State switch
            {
                GameState.Build => "拖动道具，移动旋转，开始模拟。",
                GameState.Simulate => GetSimulationStatusMessage(),
                GameState.Success => "成功!",
                GameState.Failed => "失败，正在重置...",
                _ => string.Empty
            };
            SetStatus(message);
            RefreshCostUi();

            if (runButton != null)
            {
                runButton.interactable = State == GameState.Build;
            }

            if (resetButton != null)
            {
                resetButton.interactable = State != GameState.Intro;
            }

            if (editButton != null)
            {
                editButton.interactable = State == GameState.Simulate || State == GameState.Failed || State == GameState.Success;
            }

            if (nextButton != null)
            {
                nextButton.interactable = State == GameState.Success && HasNextLevel;
            }
        }

        private void UpdateSimulationStatus()
        {
            SetStatus(GetSimulationStatusMessage());
        }

        private string GetSimulationStatusMessage()
        {
            return "\u6A21\u62DF\u4E2D...\u5269\u4F59\u65F6\u95F4\uFF1A" + Mathf.CeilToInt(simulationTimeRemaining).ToString() + "s";
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void HandleCostChanged()
        {
            RefreshCostUi();
        }

        private void RefreshCostUi()
        {
            if (hudRefs == null)
            {
                hudRefs = FindObjectOfType<GameHudRefs>();
            }

            TMP_Text costLabel = hudRefs != null ? hudRefs.CostText : null;
            if (costLabel == null)
            {
                return;
            }

            bool showCost = level != null && level.Mode == LevelMode.LowestCost;
            costLabel.gameObject.SetActive(showCost);
            if (!showCost)
            {
                return;
            }

            int currentCost = placementManager != null ? placementManager.CurrentCost : 0;
            string sceneName = SceneManager.GetActiveScene().name;
            string bestText = LowestCostProgress.TryGetBestCost(sceneName, out int bestCost) ? bestCost.ToString() : "--";
            costLabel.text = "当前花费：" + currentCost.ToString() + "　历史最低：" + bestText;
        }
    }
}
