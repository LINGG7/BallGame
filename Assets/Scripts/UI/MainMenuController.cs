using BallGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const int LevelsPerPage = 9;

        [Header("Performance")]
        [SerializeField, Min(1)] private int targetFrameRate = 60;

        [Header("Data")]
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private LevelCatalog challengeLevelCatalog;

        [Header("Main Menu")]
        [SerializeField] private Button normalButton;
        [SerializeField] private GameObject levelSelectRoot;
        [SerializeField] private RectTransform levelSelectPanel;
        [SerializeField] private Button closeLevelSelectButton;
        [SerializeField] private Button challengeButton;

        [Header("Challenge Level Select")]
        [SerializeField] private GameObject challengeLevelSelectRoot;
        [SerializeField] private RectTransform challengeLevelSelectPanel;
        [SerializeField] private Button closeChallengeLevelSelectButton;
        [SerializeField] private Button challengePreviousPageButton;
        [SerializeField] private Button challengeNextPageButton;
        [SerializeField] private TMP_Text challengePageText;
        [SerializeField] private TMP_Text challengeEmptyText;
        [SerializeField] private Button[] challengeLevelButtons = new Button[LevelsPerPage];
        [SerializeField] private TMP_Text[] challengeLevelLabels = new TMP_Text[LevelsPerPage];
        [SerializeField] private TMP_Text[] challengeBestCostLabels = new TMP_Text[LevelsPerPage];

        [Header("Level Select")]
        [SerializeField] private Button previousPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private TMP_Text pageText;
        [SerializeField] private Button[] levelButtons = new Button[LevelsPerPage];
        [SerializeField] private TMP_Text[] levelLabels = new TMP_Text[LevelsPerPage];

        private int currentPage;
        private int challengeCurrentPage;

        private int TotalLevels => levelCatalog != null ? levelCatalog.Count : 0;
        private int TotalPages => Mathf.Max(1, Mathf.CeilToInt(TotalLevels / (float)LevelsPerPage));
        private int TotalChallengeLevels => challengeLevelCatalog != null ? challengeLevelCatalog.Count : 0;
        private int TotalChallengePages => TotalChallengeLevels == 0 ? 0 : Mathf.CeilToInt(TotalChallengeLevels / (float)LevelsPerPage);

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;

            EnsureEventSystem();
            ResolvePrefabReferences();
            EnsureHapticsSettingsPanel();
            EnsureShopPanel();
            BindUi();
        }

        private void EnsureHapticsSettingsPanel()
        {
            if (GetComponent<HapticsSettingsPanelController>() == null)
            {
                gameObject.AddComponent<HapticsSettingsPanelController>();
            }
        }

        private void EnsureShopPanel()
        {
            if (GetComponent<ShopPanelController>() == null)
            {
                gameObject.AddComponent<ShopPanelController>();
            }
        }

        private void Start()
        {
            HideLevelPanel();
            HideChallengeLevelPanel();
            RefreshLevelButtons();
            RefreshChallengeLevelButtons();
        }

        private void Update()
        {
            GameObject activeRoot = levelSelectRoot != null && levelSelectRoot.activeSelf
                ? levelSelectRoot
                : challengeLevelSelectRoot != null && challengeLevelSelectRoot.activeSelf
                    ? challengeLevelSelectRoot
                    : null;
            RectTransform activePanel = activeRoot == levelSelectRoot ? levelSelectPanel : challengeLevelSelectPanel;
            if (activeRoot == null || activePanel == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && !RectTransformUtility.RectangleContainsScreenPoint(activePanel, Input.mousePosition))
            {
                HideAllLevelPanels();
                return;
            }

            if (Input.touchCount <= 0)
            {
                return;
            }

            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && !RectTransformUtility.RectangleContainsScreenPoint(activePanel, touch.position))
            {
                HideAllLevelPanels();
            }
        }

        private void BindUi()
        {
            if (normalButton != null)
            {
                normalButton.onClick.RemoveListener(ShowLevelPanel);
                normalButton.onClick.AddListener(ShowLevelPanel);
            }

            if (challengeButton != null)
            {
                challengeButton.onClick.RemoveListener(ShowChallengeLevelPanel);
                challengeButton.onClick.AddListener(ShowChallengeLevelPanel);
            }

            if (closeLevelSelectButton != null)
            {
                closeLevelSelectButton.onClick.RemoveListener(HideLevelPanel);
                closeLevelSelectButton.onClick.AddListener(HideLevelPanel);
            }

            if (previousPageButton != null)
            {
                previousPageButton.onClick.RemoveListener(ShowPreviousPage);
                previousPageButton.onClick.AddListener(ShowPreviousPage);
            }

            if (nextPageButton != null)
            {
                nextPageButton.onClick.RemoveListener(ShowNextPage);
                nextPageButton.onClick.AddListener(ShowNextPage);
            }

            if (closeChallengeLevelSelectButton != null)
            {
                closeChallengeLevelSelectButton.onClick.RemoveListener(HideChallengeLevelPanel);
                closeChallengeLevelSelectButton.onClick.AddListener(HideChallengeLevelPanel);
            }

            if (challengePreviousPageButton != null)
            {
                challengePreviousPageButton.onClick.RemoveListener(ShowPreviousChallengePage);
                challengePreviousPageButton.onClick.AddListener(ShowPreviousChallengePage);
            }

            if (challengeNextPageButton != null)
            {
                challengeNextPageButton.onClick.RemoveListener(ShowNextChallengePage);
                challengeNextPageButton.onClick.AddListener(ShowNextChallengePage);
            }
        }

        private void ResolvePrefabReferences()
        {
            if (normalButton == null)
            {
                normalButton = FindChildComponent<Button>("NormalButton");
            }

            if (challengeButton == null)
            {
                challengeButton = FindChildComponent<Button>("ChallengeButton");
            }

            if (levelSelectRoot == null)
            {
                Transform levelSelect = transform.Find("LevelSelectUI");
                if (levelSelect != null)
                {
                    levelSelectRoot = levelSelect.gameObject;
                }
            }

            if (levelSelectRoot == null)
            {
                return;
            }

            Transform panel = levelSelectRoot.transform.Find("Panel");
            if (panel == null)
            {
                return;
            }

            if (levelSelectPanel == null)
            {
                levelSelectPanel = panel.GetComponent<RectTransform>();
            }

            if (closeLevelSelectButton == null)
            {
                closeLevelSelectButton = panel.Find("CloseButton")?.GetComponent<Button>();
            }

            if (previousPageButton == null)
            {
                previousPageButton = panel.Find("PreviousPageButton")?.GetComponent<Button>();
            }

            if (nextPageButton == null)
            {
                nextPageButton = panel.Find("NextPageButton")?.GetComponent<Button>();
            }

            if (pageText == null)
            {
                pageText = panel.Find("PageText")?.GetComponent<TMP_Text>();
            }

            Transform grid = panel.Find("LevelGrid");
            if (grid == null)
            {
                return;
            }

            if (levelButtons == null || levelButtons.Length != LevelsPerPage)
            {
                levelButtons = new Button[LevelsPerPage];
            }

            if (levelLabels == null || levelLabels.Length != LevelsPerPage)
            {
                levelLabels = new TMP_Text[LevelsPerPage];
            }

            for (int i = 0; i < LevelsPerPage; i++)
            {
                Transform buttonTransform = grid.Find("LevelSlot_" + (i + 1).ToString("00"));
                if (buttonTransform == null)
                {
                    buttonTransform = grid.Find("Level_" + (i + 1).ToString("00"));
                }

                if (buttonTransform == null)
                {
                    continue;
                }

                if (levelButtons[i] == null)
                {
                    levelButtons[i] = buttonTransform.GetComponent<Button>();
                }

                if (levelLabels[i] == null)
                {
                    levelLabels[i] = buttonTransform.Find("Text")?.GetComponent<TMP_Text>();
                }
            }

            ResolveChallengePrefabReferences();
        }

        private void ResolveChallengePrefabReferences()
        {
            if (challengeLevelSelectRoot == null)
            {
                Transform challengeLevelSelect = transform.Find("ChallengeLevelSelectUI");
                if (challengeLevelSelect != null)
                {
                    challengeLevelSelectRoot = challengeLevelSelect.gameObject;
                }
            }

            if (challengeLevelSelectRoot == null)
            {
                return;
            }

            Transform panel = challengeLevelSelectRoot.transform.Find("Panel");
            if (panel == null)
            {
                return;
            }

            challengeLevelSelectPanel = challengeLevelSelectPanel != null ? challengeLevelSelectPanel : panel.GetComponent<RectTransform>();
            closeChallengeLevelSelectButton = closeChallengeLevelSelectButton != null ? closeChallengeLevelSelectButton : panel.Find("CloseButton")?.GetComponent<Button>();
            challengePreviousPageButton = challengePreviousPageButton != null ? challengePreviousPageButton : panel.Find("PreviousPageButton")?.GetComponent<Button>();
            challengeNextPageButton = challengeNextPageButton != null ? challengeNextPageButton : panel.Find("NextPageButton")?.GetComponent<Button>();
            challengePageText = challengePageText != null ? challengePageText : panel.Find("PageText")?.GetComponent<TMP_Text>();
            challengeEmptyText = challengeEmptyText != null ? challengeEmptyText : panel.Find("EmptyText")?.GetComponent<TMP_Text>();

            EnsureChallengeArrays();
            Transform grid = panel.Find("LevelGrid");
            if (grid == null)
            {
                return;
            }

            for (int i = 0; i < LevelsPerPage; i++)
            {
                Transform slot = grid.Find("LevelSlot_" + (i + 1).ToString("00"));
                if (slot == null)
                {
                    continue;
                }

                challengeLevelButtons[i] = challengeLevelButtons[i] != null ? challengeLevelButtons[i] : slot.GetComponent<Button>();
                challengeLevelLabels[i] = challengeLevelLabels[i] != null ? challengeLevelLabels[i] : slot.Find("Text")?.GetComponent<TMP_Text>();
                challengeBestCostLabels[i] = challengeBestCostLabels[i] != null ? challengeBestCostLabels[i] : slot.Find("BestCost")?.GetComponent<TMP_Text>();
            }
        }

        private void EnsureChallengeArrays()
        {
            if (challengeLevelButtons == null || challengeLevelButtons.Length != LevelsPerPage)
            {
                challengeLevelButtons = new Button[LevelsPerPage];
            }

            if (challengeLevelLabels == null || challengeLevelLabels.Length != LevelsPerPage)
            {
                challengeLevelLabels = new TMP_Text[LevelsPerPage];
            }

            if (challengeBestCostLabels == null || challengeBestCostLabels.Length != LevelsPerPage)
            {
                challengeBestCostLabels = new TMP_Text[LevelsPerPage];
            }
        }

        private T FindChildComponent<T>(string childName) where T : Component
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private void RefreshLevelButtons()
        {
            currentPage = Mathf.Clamp(currentPage, 0, TotalPages - 1);
            int startIndex = currentPage * LevelsPerPage;

            for (int i = 0; i < LevelsPerPage; i++)
            {
                Button button = levelButtons != null && i < levelButtons.Length ? levelButtons[i] : null;
                TMP_Text label = levelLabels != null && i < levelLabels.Length ? levelLabels[i] : null;
                int levelIndex = startIndex + i;
                bool hasLevel = levelCatalog != null && levelIndex >= 0 && levelIndex < levelCatalog.Count;

                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.gameObject.SetActive(hasLevel);
                }

                if (!hasLevel)
                {
                    if (label != null)
                    {
                        label.text = string.Empty;
                    }

                    continue;
                }

                LevelCatalog.LevelEntry level = levelCatalog.Levels[levelIndex];
                bool isUnlocked = LevelProgress.IsUnlocked(level.LevelNumber, levelCatalog);

                if (label != null)
                {
                    label.text = level.DisplayTitle;
                }

                if (button == null)
                {
                    continue;
                }

                button.interactable = isUnlocked;
                SetButtonColors(button, isUnlocked);
                if (isUnlocked)
                {
                    string sceneName = level.SceneName;
                    button.onClick.AddListener(() => SceneTransitionController.LoadScene(sceneName));
                }
            }

            RefreshPageControls();
        }

        private void RefreshChallengeLevelButtons()
        {
            EnsureChallengeArrays();
            int totalPages = TotalChallengePages;
            challengeCurrentPage = totalPages == 0 ? 0 : Mathf.Clamp(challengeCurrentPage, 0, totalPages - 1);
            int startIndex = challengeCurrentPage * LevelsPerPage;

            for (int i = 0; i < LevelsPerPage; i++)
            {
                Button button = challengeLevelButtons[i];
                TMP_Text titleLabel = challengeLevelLabels[i];
                TMP_Text bestCostLabel = challengeBestCostLabels[i];
                int levelIndex = startIndex + i;
                bool hasLevel = challengeLevelCatalog != null && levelIndex >= 0 && levelIndex < challengeLevelCatalog.Count;

                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.gameObject.SetActive(hasLevel);
                }

                if (!hasLevel)
                {
                    if (titleLabel != null)
                    {
                        titleLabel.text = string.Empty;
                    }

                    if (bestCostLabel != null)
                    {
                        bestCostLabel.gameObject.SetActive(false);
                    }

                    continue;
                }

                LevelCatalog.LevelEntry level = challengeLevelCatalog.Levels[levelIndex];
                bool isUnlocked = ChallengeProgress.IsUnlocked(level.LevelNumber, challengeLevelCatalog);
                if (titleLabel != null)
                {
                    titleLabel.text = level.DisplayTitle;
                }

                if (bestCostLabel != null)
                {
                    bestCostLabel.gameObject.SetActive(isUnlocked);
                    if (isUnlocked)
                    {
                        bestCostLabel.text = LowestCostProgress.TryGetBestCost(level.SceneName, out int bestCost)
                            ? "历史最低：" + bestCost.ToString()
                            : "历史最低：未挑战";
                    }
                }

                if (button == null)
                {
                    continue;
                }

                button.interactable = isUnlocked;
                SetButtonColors(button, isUnlocked);
                if (isUnlocked)
                {
                    string sceneName = level.SceneName;
                    button.onClick.AddListener(() => SceneTransitionController.LoadScene(sceneName));
                }
            }

            if (challengeEmptyText != null)
            {
                challengeEmptyText.gameObject.SetActive(TotalChallengeLevels == 0);
                challengeEmptyText.text = "暂无挑战关卡";
            }

            RefreshChallengePageControls();
        }

        private void RefreshPageControls()
        {
            int totalPages = TotalPages;
            if (pageText != null)
            {
                pageText.text = currentPage + 1 + "/" + totalPages;
            }

            if (previousPageButton != null)
            {
                previousPageButton.interactable = currentPage > 0;
            }

            if (nextPageButton != null)
            {
                nextPageButton.interactable = currentPage < totalPages - 1;
            }
        }

        private void RefreshChallengePageControls()
        {
            int totalPages = TotalChallengePages;
            if (challengePageText != null)
            {
                challengePageText.text = totalPages == 0 ? "0/0" : challengeCurrentPage + 1 + "/" + totalPages;
            }

            if (challengePreviousPageButton != null)
            {
                challengePreviousPageButton.gameObject.SetActive(totalPages > 0);
                challengePreviousPageButton.interactable = totalPages > 0 && challengeCurrentPage > 0;
            }

            if (challengeNextPageButton != null)
            {
                challengeNextPageButton.gameObject.SetActive(totalPages > 0);
                challengeNextPageButton.interactable = totalPages > 0 && challengeCurrentPage < totalPages - 1;
            }
        }

        private void ShowPreviousPage()
        {
            if (currentPage <= 0)
            {
                return;
            }

            currentPage--;
            RefreshLevelButtons();
        }

        private void ShowNextPage()
        {
            if (currentPage >= TotalPages - 1)
            {
                return;
            }

            currentPage++;
            RefreshLevelButtons();
        }

        private void ShowPreviousChallengePage()
        {
            if (challengeCurrentPage <= 0)
            {
                return;
            }

            challengeCurrentPage--;
            RefreshChallengeLevelButtons();
        }

        private void ShowNextChallengePage()
        {
            if (challengeCurrentPage >= TotalChallengePages - 1)
            {
                return;
            }

            challengeCurrentPage++;
            RefreshChallengeLevelButtons();
        }

        private void ShowLevelPanel()
        {
            HideChallengeLevelPanel();
            currentPage = Mathf.Clamp(currentPage, 0, TotalPages - 1);
            RefreshLevelButtons();
            if (levelSelectRoot != null)
            {
                levelSelectRoot.SetActive(true);
                levelSelectRoot.transform.SetAsLastSibling();
            }
        }

        private void HideLevelPanel()
        {
            if (levelSelectRoot != null)
            {
                levelSelectRoot.SetActive(false);
            }
        }

        private void ShowChallengeLevelPanel()
        {
            HideLevelPanel();
            int totalPages = TotalChallengePages;
            challengeCurrentPage = totalPages == 0 ? 0 : Mathf.Clamp(challengeCurrentPage, 0, totalPages - 1);
            RefreshChallengeLevelButtons();
            if (challengeLevelSelectRoot != null)
            {
                challengeLevelSelectRoot.SetActive(true);
                challengeLevelSelectRoot.transform.SetAsLastSibling();
            }
        }

        private void HideChallengeLevelPanel()
        {
            if (challengeLevelSelectRoot != null)
            {
                challengeLevelSelectRoot.SetActive(false);
            }
        }

        private void HideAllLevelPanels()
        {
            HideLevelPanel();
            HideChallengeLevelPanel();
        }

        private void SetButtonColors(Button button, bool unlocked)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = unlocked ? Color.white : new Color(0.56f, 0.56f, 0.56f, 0.78f);
            colors.highlightedColor = unlocked ? new Color(1f, 0.94f, 0.76f, 1f) : colors.normalColor;
            colors.pressedColor = unlocked ? new Color(0.86f, 0.74f, 0.48f, 1f) : colors.normalColor;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.72f);
            button.colors = colors;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
