using System.Collections;
using BallGame.Audio;
using BallGame.Core;
using BallGame.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.UI
{
    public sealed class LevelCompletePopup : MonoBehaviour
    {
        private const string LevelCompletedResourcePath = "Audio/SFX/LevelCompleted";
        private const string FireworkLauncherResourcePath = "Prefabs/FireworkLauncher";
        private const int MinimumSuccessFireworks = 3;
        private const int MaximumSuccessFireworksExclusive = 6;

        [SerializeField] private GameManager gameManager;
        [SerializeField] private RectTransform popupPrefab;
        [SerializeField] private RectTransform overlayRoot;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text costSummaryText;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button nextButton;

        private Coroutine showRoutine;
        private AudioClip levelCompletedSfx;
        private FireworkLauncher fireworkLauncherPrefab;
        private FireworkLauncher activeSuccessFireworkLauncher;
        private bool isShown;

        private void Awake()
        {
            levelCompletedSfx = Resources.Load<AudioClip>(LevelCompletedResourcePath);
            GameObject launcherPrefabObject = Resources.Load<GameObject>(FireworkLauncherResourcePath);
            fireworkLauncherPrefab = launcherPrefabObject != null
                ? launcherPrefabObject.GetComponent<FireworkLauncher>()
                : null;
            EnsurePrefabUi();
            BindButtons();
            HideImmediate();
        }

        private void Start()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            if (gameManager != null)
            {
                gameManager.StateChanged += HandleStateChanged;
                HandleStateChanged(gameManager.State);
            }
        }

        private void OnDestroy()
        {
            StopSuccessFireworks();
            if (gameManager != null)
            {
                gameManager.StateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Success)
            {
                Show();
            }
            else
            {
                HideImmediate();
            }
        }

        private void Show()
        {
            EnsurePrefabUi();
            BindButtons();
            if (overlayRoot == null)
            {
                return;
            }

            if (nextButton != null)
            {
                nextButton.interactable = gameManager != null && gameManager.HasNextLevel;
            }

            if (titleText != null)
            {
                string popupTitle = gameManager != null ? gameManager.CurrentCompletionResult.PopupTitle : null;
                titleText.text = string.IsNullOrWhiteSpace(popupTitle) ? LevelCompletionResult.DefaultPopupTitle : popupTitle;
            }

            RefreshCostSummary();

            overlayRoot.gameObject.SetActive(true);
            overlayRoot.SetAsLastSibling();
            if (TryMarkShown())
            {
                PlayLevelCompletedSound();
                PlaySuccessFireworks();
            }

            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
            }

            if (canvasGroup != null && panelRoot != null)
            {
                showRoutine = StartCoroutine(PlayShowAnimation());
            }
        }

        private void HideImmediate()
        {
            isShown = false;
            StopSuccessFireworks();
            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (panelRoot != null)
            {
                panelRoot.localScale = Vector3.one;
            }

            if (overlayRoot != null)
            {
                overlayRoot.gameObject.SetActive(false);
            }
        }

        private bool TryMarkShown()
        {
            if (isShown)
            {
                return false;
            }

            isShown = true;
            return true;
        }

        private void PlayLevelCompletedSound()
        {
            AudioManager.Instance?.PlaySfx(levelCompletedSfx);
        }

        private void PlaySuccessFireworks()
        {
            StopSuccessFireworks();
            if (fireworkLauncherPrefab == null)
            {
                return;
            }

            activeSuccessFireworkLauncher = Instantiate(fireworkLauncherPrefab);
            activeSuccessFireworkLauncher.name = fireworkLauncherPrefab.name;
            activeSuccessFireworkLauncher.ConfigureForCamera(Camera.main);
            activeSuccessFireworkLauncher.PlayOnce(GetSuccessFireworkCount());
        }

        private void StopSuccessFireworks()
        {
            if (activeSuccessFireworkLauncher == null)
            {
                return;
            }

            FireworkLauncher launcher = activeSuccessFireworkLauncher;
            activeSuccessFireworkLauncher = null;
            launcher.Stop();
            if (Application.isPlaying)
            {
                Destroy(launcher.gameObject);
            }
            else
            {
                DestroyImmediate(launcher.gameObject);
            }
        }

        private static int GetSuccessFireworkCount()
        {
            return Random.Range(MinimumSuccessFireworks, MaximumSuccessFireworksExclusive);
        }

        private IEnumerator PlayShowAnimation()
        {
            const float duration = 0.22f;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            panelRoot.localScale = Vector3.one * 0.72f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = t;

                float scale = t < 0.72f
                    ? Mathf.Lerp(0.72f, 1.08f, t / 0.72f)
                    : Mathf.Lerp(1.08f, 1f, (t - 0.72f) / 0.28f);
                panelRoot.localScale = Vector3.one * scale;
                yield return null;
            }

            canvasGroup.alpha = 1f;
            panelRoot.localScale = Vector3.one;
            showRoutine = null;
        }

        private void EnsurePrefabUi()
        {
            if (overlayRoot == null && popupPrefab != null)
            {
                overlayRoot = Instantiate(popupPrefab, transform);
                overlayRoot.name = popupPrefab.name;
            }

            if (overlayRoot == null)
            {
                Debug.LogWarning("LevelCompletePopup is missing popup prefab or overlay reference.", this);
                return;
            }

            overlayRoot.SetAsLastSibling();

            if (canvasGroup == null)
            {
                canvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            }

            if (panelRoot == null)
            {
                Transform panel = overlayRoot.Find("Panel");
                panelRoot = panel != null ? panel as RectTransform : null;
            }

            if (menuButton == null)
            {
                menuButton = FindButton("MenuButton");
            }

            if (nextButton == null)
            {
                nextButton = FindButton("NextButton");
            }

            if (retryButton == null)
            {
                retryButton = FindButton("RetryButton");
            }

            if (titleText == null && panelRoot != null)
            {
                Transform title = panelRoot.Find("Title");
                titleText = title != null ? title.GetComponent<TMP_Text>() : null;
            }

            EnsureCostSummaryText();
        }

        private void EnsureCostSummaryText()
        {
            if (costSummaryText != null || panelRoot == null)
            {
                return;
            }

            Transform summary = panelRoot.Find("CostSummary");
            if (summary != null)
            {
                costSummaryText = summary.GetComponent<TMP_Text>();
                return;
            }

            if (titleText == null)
            {
                return;
            }

            costSummaryText = Instantiate(titleText, panelRoot);
            costSummaryText.name = "CostSummary";
            costSummaryText.fontSize = 28f;
            costSummaryText.fontSizeMax = 28f;
            costSummaryText.fontSizeMin = 16f;
            RectTransform rect = costSummaryText.rectTransform;
            rect.anchoredPosition = new Vector2(0f, -175f);
            rect.sizeDelta = new Vector2(500f, 92f);
        }

        private void RefreshCostSummary()
        {
            EnsureCostSummaryText();
            if (costSummaryText == null)
            {
                return;
            }

            LevelCompletionResult result = gameManager != null ? gameManager.CurrentCompletionResult : null;
            bool showCost = result != null && result.HasCostResult;
            costSummaryText.gameObject.SetActive(showCost);
            if (!showCost)
            {
                return;
            }

            costSummaryText.text = (result.IsNewBestCost ? "新纪录！" : string.Empty)
                + "\n本次花费：" + result.CurrentCost.ToString()
                + "　历史最低：" + result.BestCost.ToString();
        }

        private void BindButtons()
        {
            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(LoadMainMenu);
                menuButton.onClick.AddListener(LoadMainMenu);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(LoadNextLevel);
                nextButton.onClick.AddListener(LoadNextLevel);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(RetryLevel);
                retryButton.onClick.AddListener(RetryLevel);
            }
        }

        private Button FindButton(string buttonName)
        {
            if (overlayRoot == null)
            {
                return null;
            }

            Transform buttonTransform = overlayRoot.Find(buttonName);
            if (buttonTransform == null && panelRoot != null)
            {
                buttonTransform = panelRoot.Find(buttonName);
            }

            return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        }

        private static void LoadMainMenu()
        {
            SceneTransitionController.LoadScene("MainMenu");
        }

        private void LoadNextLevel()
        {
            gameManager?.LoadNextLevel();
        }

        private void RetryLevel()
        {
            gameManager?.RetryLevelAndClearTools();
        }
    }
}
