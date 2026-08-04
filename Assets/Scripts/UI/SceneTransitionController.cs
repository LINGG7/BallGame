using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BallGame.UI
{
    public sealed class SceneTransitionController : MonoBehaviour
    {
        private const float TransitionDuration = 0.55f;
        private const int OverlaySortingOrder = short.MaxValue;
        private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
        private static readonly int DissolveProgressId = Shader.PropertyToID("_DissolveProgress");
        private static readonly int OriginId = Shader.PropertyToID("_Origin");
        private static readonly int NoiseTextureId = Shader.PropertyToID("_NoiseTex");

        private static SceneTransitionController instance;

        private GameObject overlayRoot;
        private Material transitionMaterial;
        private bool isTransitioning;

        public static bool IsTransitioning => instance != null && instance.isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static void LoadScene(string sceneName)
        {
            SceneTransitionController controller = EnsureInstance();
            if (controller.isTransitioning)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("Cannot transition to an empty scene name.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Cannot transition to scene '{sceneName}' because it is not available in Build Settings.");
                return;
            }

            controller.SetOrigin(CapturePointerOrigin());
            controller.StartCoroutine(controller.RunTransition(sceneName));
        }

        private static Vector2 CapturePointerOrigin()
        {
            Vector2 screenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            bool hasPointerPosition = false;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Canceled)
                {
                    screenPosition = touch.position;
                    hasPointerPosition = true;
                }
            }
            else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
            {
                screenPosition = Input.mousePosition;
                hasPointerPosition = true;
            }

            if (!hasPointerPosition || Screen.width <= 0 || Screen.height <= 0
                || screenPosition.x < 0f || screenPosition.x > Screen.width
                || screenPosition.y < 0f || screenPosition.y > Screen.height)
            {
                return new Vector2(0.5f, 0.5f);
            }

            return new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
        }

        private static SceneTransitionController EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject controllerObject = new GameObject("SceneTransitionController");
            controllerObject.SetActive(false);
            instance = controllerObject.AddComponent<SceneTransitionController>();
            DontDestroyOnLoad(controllerObject);
            controllerObject.SetActive(true);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        private void BuildOverlay()
        {
            overlayRoot = new GameObject(
                "InkTransitionOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            overlayRoot.transform.SetParent(transform, false);

            Canvas canvas = overlayRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            GameObject inkObject = new GameObject(
                "Ink",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform inkRect = inkObject.GetComponent<RectTransform>();
            inkRect.SetParent(overlayRoot.transform, false);
            inkRect.anchorMin = Vector2.zero;
            inkRect.anchorMax = Vector2.one;
            inkRect.pivot = new Vector2(0.5f, 0.5f);
            inkRect.offsetMin = Vector2.zero;
            inkRect.offsetMax = Vector2.zero;

            Image inkImage = inkObject.GetComponent<Image>();
            inkImage.color = new Color(0.035f, 0.03f, 0.025f, 1f);
            inkImage.raycastTarget = true;

            Shader shader = Resources.Load<Shader>("Shaders/UIInkSceneTransition");
            if (shader == null)
            {
                shader = Shader.Find("BallGame/UI/Ink Scene Transition");
            }

            if (shader != null)
            {
                transitionMaterial = new Material(shader)
                {
                    name = "Ink Scene Transition (Instance)"
                };
                Texture2D noiseTexture = Resources.Load<Texture2D>("Textures/InkTransitionNoise");
                if (noiseTexture != null)
                {
                    transitionMaterial.SetTexture(NoiseTextureId, noiseTexture);
                }
                else
                {
                    Debug.LogWarning("Ink transition noise texture could not be found. Using the shader fallback texture.");
                }

                inkImage.material = transitionMaterial;
                SetTransitionState(0f, 0f);
                SetOrigin(new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogError("Ink scene transition shader could not be found. Falling back to a solid overlay.");
            }

            overlayRoot.SetActive(false);
        }

        private IEnumerator RunTransition(string sceneName)
        {
            isTransitioning = true;
            overlayRoot.SetActive(true);
            SetTransitionState(0f, 0f);
            yield return AnimateMaterialProperty(RevealProgressId, 0f, 1f);
            SetTransitionState(1f, 0f);

            AsyncOperation loadOperation = null;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Failed to start loading scene '{sceneName}': {exception.Message}");
            }

            if (loadOperation == null)
            {
                yield return AnimateMaterialProperty(DissolveProgressId, 0f, 1f);
                FinishTransition();
                yield break;
            }

            // Keep activation on hold until the scene data is ready and the screen is fully covered.
            loadOperation.allowSceneActivation = false;
            while (loadOperation.progress < 0.9f)
            {
                yield return null;
            }

            yield return null;
            loadOperation.allowSceneActivation = true;
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            // Let the newly activated scene finish one frame of initialization while fully covered.
            yield return null;

            yield return AnimateMaterialProperty(DissolveProgressId, 0f, 1f);
            FinishTransition();
        }

        private IEnumerator AnimateMaterialProperty(int propertyId, float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < TransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / TransitionDuration);
                float eased = normalized * normalized * (3f - 2f * normalized);
                SetMaterialFloat(propertyId, Mathf.LerpUnclamped(from, to, eased));
                yield return null;
            }

            SetMaterialFloat(propertyId, to);
        }

        private void SetTransitionState(float revealProgress, float dissolveProgress)
        {
            SetMaterialFloat(RevealProgressId, revealProgress);
            SetMaterialFloat(DissolveProgressId, dissolveProgress);
        }

        private void SetOrigin(Vector2 origin)
        {
            if (transitionMaterial != null)
            {
                transitionMaterial.SetVector(OriginId, new Vector4(origin.x, origin.y, 0f, 0f));
            }
        }

        private void SetMaterialFloat(int propertyId, float value)
        {
            if (transitionMaterial != null)
            {
                transitionMaterial.SetFloat(propertyId, Mathf.Clamp01(value));
            }
        }

        private void FinishTransition()
        {
            SetTransitionState(0f, 0f);
            overlayRoot.SetActive(false);
            isTransitioning = false;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (transitionMaterial != null)
            {
                Destroy(transitionMaterial);
            }
        }
    }
}
