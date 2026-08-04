using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.UI
{
    public sealed class TutorialOverlay : MonoBehaviour
    {
        [System.Serializable]
        public struct Step
        {
            public RectTransform target;
            [TextArea(2, 4)] public string message;
            public float arrowRotation;
            public Vector2 arrowOffset;
            public Vector2 textOffset;
        }

        [SerializeField] private bool playOnStart;
        [SerializeField] private Step[] steps;
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Sprite messageSprite;
        [SerializeField] private TMP_FontAsset tutorialFont;
        [SerializeField] private Vector2 arrowSize = new Vector2(96f, 96f);
        [SerializeField] private Vector2 textBoxSize = new Vector2(460f, 118f);

        private RectTransform overlayRoot;
        private CanvasGroup canvasGroup;
        private Button clickCatcher;
        private RectTransform arrowTransform;
        private RectTransform messageRoot;
        private TMP_Text messageText;
        private int currentStepIndex = -1;
        private Coroutine pendingPlay;

        private void Start()
        {
            EnsureRuntimeUi();
            Hide();
            if (playOnStart && steps != null && steps.Length > 0)
            {
                QueuePlayAfterIntro();
            }
        }

        public void Play()
        {
            EnsureRuntimeUi();
            if (steps == null || steps.Length == 0)
            {
                Hide();
                return;
            }

            QueuePlayAfterIntro();
        }

        private void QueuePlayAfterIntro()
        {
            if (pendingPlay != null)
            {
                StopCoroutine(pendingPlay);
            }

            pendingPlay = StartCoroutine(PlayAfterIntro());
        }

        private IEnumerator PlayAfterIntro()
        {
            // Give every Start method one frame to initialize so script execution
            // order cannot make the tutorial appear before the level intro begins.
            yield return null;

            LevelIntroController intro = GetComponent<LevelIntroController>();
            if (intro == null)
            {
                intro = FindObjectOfType<LevelIntroController>();
            }

            while (intro != null && intro.IsPlaying)
            {
                yield return null;
            }

            pendingPlay = null;
            ShowStep(0);
        }

        public void Hide()
        {
            if (pendingPlay != null)
            {
                StopCoroutine(pendingPlay);
                pendingPlay = null;
            }

            currentStepIndex = -1;
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void Advance()
        {
            int nextIndex = currentStepIndex + 1;
            if (steps == null || nextIndex >= steps.Length)
            {
                Hide();
                return;
            }

            ShowStep(nextIndex);
        }

        private void ShowStep(int index)
        {
            EnsureRuntimeUi();
            if (steps == null || index < 0 || index >= steps.Length)
            {
                Hide();
                return;
            }

            currentStepIndex = index;
            Step step = steps[index];
            Vector2 targetPosition = GetTargetLocalPosition(step.target);

            arrowTransform.anchoredPosition = targetPosition + step.arrowOffset;
            arrowTransform.localEulerAngles = new Vector3(0f, 0f, step.arrowRotation);
            messageRoot.anchoredPosition = targetPosition + step.textOffset;
            messageText.text = step.message;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        private Vector2 GetTargetLocalPosition(RectTransform target)
        {
            if (target == null)
            {
                return Vector2.zero;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldCenter);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPoint, canvasCamera, out Vector2 localPoint);
            return localPoint;
        }

        private void EnsureRuntimeUi()
        {
            if (overlayRoot != null)
            {
                return;
            }

            overlayRoot = CreateRect("TutorialOverlay_Runtime", transform);
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;
            overlayRoot.SetAsLastSibling();

            Image background = overlayRoot.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.5f);
            background.raycastTarget = true;

            canvasGroup = overlayRoot.gameObject.AddComponent<CanvasGroup>();
            clickCatcher = overlayRoot.gameObject.AddComponent<Button>();
            clickCatcher.transition = Selectable.Transition.None;
            clickCatcher.targetGraphic = background;
            clickCatcher.onClick.AddListener(Advance);

            arrowTransform = CreateRect("Arrow", overlayRoot);
            arrowTransform.sizeDelta = arrowSize;
            Image arrowImage = arrowTransform.gameObject.AddComponent<Image>();
            arrowImage.sprite = arrowSprite;
            arrowImage.color = Color.white;
            arrowImage.preserveAspect = true;
            arrowImage.raycastTarget = false;

            messageRoot = CreateRect("Message", overlayRoot);
            messageRoot.sizeDelta = textBoxSize;
            Image messageBackground = messageRoot.gameObject.AddComponent<Image>();
            messageBackground.color = new Color(1f, 1f, 1f, 0.92f);
            messageBackground.raycastTarget = false;
            messageBackground.sprite = messageSprite;
            messageBackground.type = Image.Type.Sliced;

            RectTransform textTransform = CreateRect("Text", messageRoot);
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = new Vector2(24f, 16f);
            textTransform.offsetMax = new Vector2(-24f, -16f);

            messageText = textTransform.gameObject.AddComponent<TextMeshProUGUI>();
            if (tutorialFont != null)
            {
                messageText.font = tutorialFont;
            }

            messageText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            messageText.fontSize = 30f;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 18f;
            messageText.fontSizeMax = 30f;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject instance = new GameObject(name, typeof(RectTransform));
            instance.transform.SetParent(parent, false);
            return instance.GetComponent<RectTransform>();
        }
    }
}
