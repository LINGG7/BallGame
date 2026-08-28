using UnityEngine;
using UnityEngine.UI;

namespace BallGame.Core
{
    public sealed class LevelTestVisual3DBootstrap : MonoBehaviour
    {
        private const float HorizontalVelocityEpsilon = 0.01f;

        private enum CameraTransition
        {
            None,
            To3D,
            To2D
        }

        private BallController ball;
        private BallVisual3DTransition visual;
        private CameraController2D cameraController;
        private GameManager gameManager;
        private Camera mainCamera;
        private Camera perspectiveCamera;
        private Camera transitionOutputCamera;
        private CameraTransition activeTransition;
        private RenderTexture twoDTexture;
        private RenderTexture threeDTexture;
        private Canvas blendCanvas;
        private RawImage twoDFrame;
        private RawImage threeDFrame;
        private Vector3 transitionAnchorPosition;
        private Vector3 transitionFromPosition;
        private Quaternion transitionFromRotation;
        private Vector3 transitionTargetPosition;
        private Quaternion transitionTargetRotation;
        private Vector3 perspectiveFollowCenter;
        private Vector3 activePerspectiveOffset;
        private Vector3 followVelocity;
        private float transitionTime;
        private float originalFov;
        private float originalOrthographicSize;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private bool is3D;
        private bool transitionVisualSwitched;
        private bool cameraStateCaptured;

        [SerializeField] private Vector3 perspectiveOffset = new Vector3(-1f, 2.5f, -10f);
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private float perspectiveFov = 52f;
        [SerializeField] private float transitionDuration = 0.8f;
        [SerializeField] private float followSmoothTime = 0.12f;
        [SerializeField] private float followRotationSmoothSpeed = 8f;
        [SerializeField, Min(0f)] private float perspectiveOffsetFlipSpeed = 4f;
        [SerializeField] private Vector2 perspectiveDeadZone = new Vector2(1.8f, 1.1f);
        [SerializeField, Range(0.25f, 1f)] private float renderTextureScale = 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForScenesWithVisualStageDoors()
        {
            if (FindObjectOfType<LevelTestVisual3DBootstrap>() != null) return;
            if (FindObjectOfType<LevelTestDoorTrigger>() == null) return;
            new GameObject("Visual Stage Transition").AddComponent<LevelTestVisual3DBootstrap>();
        }

        private void Start()
        {
            ball = FindObjectOfType<BallController>();
            cameraController = FindObjectOfType<CameraController2D>();
            gameManager = FindObjectOfType<GameManager>();
            mainCamera = Camera.main;
            if (mainCamera == null) return;
            originalFov = mainCamera.fieldOfView;
            originalOrthographicSize = mainCamera.orthographicSize;
            originalCameraPosition = mainCamera.transform.position;
            originalCameraRotation = mainCamera.transform.rotation;
            cameraStateCaptured = true;
            if (ball == null) return;

            visual = ball.GetComponent<BallVisual3DTransition>();
            if (visual == null)
            {
                Debug.LogError("Ball prefab is missing BallVisual3DTransition; the 3D transition is disabled.", ball);
                return;
            }

            visual.Initialize();
            activePerspectiveOffset = perspectiveOffset;
            CreateTransitionResources();
            if (gameManager != null) gameManager.StateChanged += HandleStateChanged;

            LevelTestDoorTrigger[] placedDoors = FindObjectsOfType<LevelTestDoorTrigger>();
            for (int i = 0; i < placedDoors.Length; i++) placedDoors[i].Initialize(this);
        }

        private void CreateTransitionResources()
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * renderTextureScale));
            int height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * renderTextureScale));
            twoDTexture = CreateTexture("VisualStage2D", width, height);
            threeDTexture = CreateTexture("VisualStage3D", width, height);

            GameObject cameraObject = new GameObject("Visual Stage 3D Camera");
            perspectiveCamera = cameraObject.AddComponent<Camera>();
            perspectiveCamera.CopyFrom(mainCamera);
            perspectiveCamera.targetTexture = threeDTexture;
            perspectiveCamera.orthographic = false;
            perspectiveCamera.fieldOfView = perspectiveFov;
            perspectiveCamera.enabled = false;

            GameObject outputObject = new GameObject("Visual Stage Output Camera");
            transitionOutputCamera = outputObject.AddComponent<Camera>();
            transitionOutputCamera.clearFlags = CameraClearFlags.SolidColor;
            transitionOutputCamera.backgroundColor = Color.black;
            transitionOutputCamera.cullingMask = 0;
            transitionOutputCamera.depth = mainCamera.depth - 1f;
            transitionOutputCamera.enabled = false;

            GameObject canvasObject = new GameObject("Visual Stage Blend", typeof(Canvas));
            blendCanvas = canvasObject.GetComponent<Canvas>();
            blendCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            blendCanvas.sortingOrder = -1000;
            twoDFrame = CreateFrame(canvasObject.transform, "2DFrame", twoDTexture);
            threeDFrame = CreateFrame(canvasObject.transform, "3DFrame", threeDTexture);
            twoDFrame.color = Color.white;
            threeDFrame.color = new Color(1f, 1f, 1f, 0f);
            blendCanvas.enabled = false;
        }

        private static RenderTexture CreateTexture(string name, int width, int height)
        {
            RenderTexture texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false
            };
            texture.Create();
            return texture;
        }

        private static RawImage CreateFrame(Transform parent, string name, Texture texture)
        {
            GameObject frameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            frameObject.transform.SetParent(parent, false);
            RectTransform rect = frameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            RawImage frame = frameObject.GetComponent<RawImage>();
            frame.texture = texture;
            frame.raycastTarget = false;
            return frame;
        }

        private void Update()
        {
            if (activeTransition == CameraTransition.None || ball == null) return;
            transitionTime += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTime / Mathf.Max(0.01f, transitionDuration));
            t = t * t * (3f - 2f * t);
            UpdateTransition(t);
        }

        private void LateUpdate()
        {
            if (activeTransition == CameraTransition.None && is3D && perspectiveCamera != null)
            {
                FollowBallIn3D();
            }
        }

        public void Enter3D()
        {
            Enter3D(null);
        }

        public void Enter3D(LevelTestDoorTrigger entryDoor)
        {
            if (activeTransition != CameraTransition.None || is3D || ball == null || perspectiveCamera == null) return;
            activeTransition = CameraTransition.To3D;
            transitionTime = 0f;
            transitionAnchorPosition = ball.transform.position;
            transitionFromPosition = mainCamera.transform.position;
            transitionFromRotation = mainCamera.transform.rotation;
            Vector3 resolvedPerspectiveOffset = perspectiveOffset;
            if (entryDoor != null)
            {
                // Compare world-space X positions: left of the door uses -1,
                // right of the door uses 1.
                resolvedPerspectiveOffset.x = transitionAnchorPosition.x < entryDoor.transform.position.x
                    ? -1f
                    : 1f;
            }

            activePerspectiveOffset = resolvedPerspectiveOffset;
            transitionTargetPosition = transitionAnchorPosition + activePerspectiveOffset;
            transitionTargetRotation = Quaternion.LookRotation(
                transitionAnchorPosition + lookAtOffset - transitionTargetPosition, Vector3.up);
            transitionVisualSwitched = false;
            mainCamera.targetTexture = twoDTexture;
            perspectiveCamera.transform.SetPositionAndRotation(transitionFromPosition, transitionFromRotation);
            perspectiveCamera.fieldOfView = perspectiveFov;
            perspectiveCamera.enabled = true;
            transitionOutputCamera.enabled = true;
            blendCanvas.enabled = true;
            threeDFrame.color = new Color(1f, 1f, 1f, 0f);
            if (cameraController != null) cameraController.enabled = false;
        }

        public void Enter2D()
        {
            if (activeTransition != CameraTransition.None || !is3D || ball == null || perspectiveCamera == null) return;
            activeTransition = CameraTransition.To2D;
            transitionTime = 0f;
            transitionAnchorPosition = ball.transform.position;
            // mainCamera is the camera that follows the ball while 3D mode is active.
            // perspectiveCamera is disabled during that phase and may still contain
            // the pose captured when entering 3D (often pointing at VisualTo3DDoor).
            transitionFromPosition = mainCamera.transform.position;
            transitionFromRotation = mainCamera.transform.rotation;
            transitionTargetPosition = new Vector3(transitionAnchorPosition.x, transitionAnchorPosition.y, originalCameraPosition.z);
            transitionTargetRotation = originalCameraRotation;
            transitionVisualSwitched = false;
            mainCamera.targetTexture = twoDTexture;
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = originalOrthographicSize;
            mainCamera.transform.SetPositionAndRotation(transitionFromPosition, transitionFromRotation);
            perspectiveCamera.transform.SetPositionAndRotation(transitionFromPosition, transitionFromRotation);
            threeDFrame.color = Color.white;
            twoDFrame.color = new Color(1f, 1f, 1f, 0f);
            mainCamera.enabled = true;
            perspectiveCamera.enabled = true;
            transitionOutputCamera.enabled = true;
            blendCanvas.enabled = true;
        }

        private void UpdateTransition(float t)
        {
            float moveT = t * t * (3f - 2f * t);
            Camera movingCamera = activeTransition == CameraTransition.To3D ? perspectiveCamera : mainCamera;
            movingCamera.transform.position = Vector3.Lerp(transitionFromPosition, transitionTargetPosition, moveT);
            movingCamera.transform.rotation = Quaternion.Slerp(transitionFromRotation, transitionTargetRotation, moveT);

            if (activeTransition == CameraTransition.To3D)
            {
                threeDFrame.color = new Color(1f, 1f, 1f, moveT);
                twoDFrame.color = new Color(1f, 1f, 1f, 1f - moveT);
            }
            else
            {
                threeDFrame.color = new Color(1f, 1f, 1f, 1f - moveT);
                twoDFrame.color = new Color(1f, 1f, 1f, moveT);
            }

            if (!transitionVisualSwitched && t >= 0.5f)
            {
                transitionVisualSwitched = true;
                if (activeTransition == CameraTransition.To3D) visual.SwitchTo3D();
                else visual.ResetTo2D();
            }

            if (t >= 1f) CompleteTransition();
        }

        private void CompleteTransition()
        {
            if (activeTransition == CameraTransition.To3D)
            {
                mainCamera.transform.SetPositionAndRotation(perspectiveCamera.transform.position, perspectiveCamera.transform.rotation);
                mainCamera.orthographic = false;
                mainCamera.fieldOfView = perspectiveFov;
                if (!transitionVisualSwitched) visual.SwitchTo3D();
                perspectiveFollowCenter = mainCamera.transform.position - activePerspectiveOffset;
                is3D = true;
            }
            else
            {
                mainCamera.transform.SetPositionAndRotation(transitionTargetPosition, transitionTargetRotation);
                mainCamera.orthographic = true;
                mainCamera.fieldOfView = originalFov;
                mainCamera.orthographicSize = originalOrthographicSize;
                if (!transitionVisualSwitched) visual.ResetTo2D();
                is3D = false;
                if (cameraController != null)
                {
                    cameraController.enabled = true;
                    cameraController.FocusOnBall(false);
                }
            }

            perspectiveCamera.enabled = false;
            transitionOutputCamera.enabled = false;
            mainCamera.targetTexture = null;
            blendCanvas.enabled = false;
            activeTransition = CameraTransition.None;
        }

        private void FollowBallIn3D()
        {
            float horizontalVelocity = ball.Body != null ? ball.Body.velocity.x : 0f;
            if (horizontalVelocity > HorizontalVelocityEpsilon)
            {
                activePerspectiveOffset.x = Mathf.MoveTowards(
                    activePerspectiveOffset.x, -1f, perspectiveOffsetFlipSpeed * Time.deltaTime);
            }
            else if (horizontalVelocity < -HorizontalVelocityEpsilon)
            {
                activePerspectiveOffset.x = Mathf.MoveTowards(
                    activePerspectiveOffset.x, 1f, perspectiveOffsetFlipSpeed * Time.deltaTime);
            }

            Vector3 ballPosition = ball.transform.position;
            Vector3 delta = ballPosition - perspectiveFollowCenter;
            if (Mathf.Abs(delta.x) > perspectiveDeadZone.x)
                perspectiveFollowCenter.x = ballPosition.x - Mathf.Sign(delta.x) * perspectiveDeadZone.x;
            if (Mathf.Abs(delta.y) > perspectiveDeadZone.y)
                perspectiveFollowCenter.y = ballPosition.y - Mathf.Sign(delta.y) * perspectiveDeadZone.y;
            perspectiveFollowCenter.z = ballPosition.z;
            Vector3 desiredPosition = perspectiveFollowCenter + activePerspectiveOffset;
            Vector3 target = perspectiveFollowCenter + lookAtOffset;
            mainCamera.transform.position = Vector3.SmoothDamp(mainCamera.transform.position, desiredPosition, ref followVelocity, followSmoothTime);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, Quaternion.LookRotation(target - desiredPosition, Vector3.up), 1f - Mathf.Exp(-followRotationSmoothSpeed * Time.deltaTime));
            mainCamera.fieldOfView = perspectiveFov;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Build && (is3D || activeTransition != CameraTransition.None)) Restore2DImmediate();
        }

        public void Restore2DImmediate()
        {
            activeTransition = CameraTransition.None;
            is3D = false;
            if (visual != null) visual.ResetTo2D();
            if (mainCamera != null && cameraStateCaptured)
            {
                mainCamera.targetTexture = null;
                mainCamera.orthographic = true;
                mainCamera.fieldOfView = originalFov;
                mainCamera.orthographicSize = originalOrthographicSize;
                mainCamera.transform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
            }
            if (perspectiveCamera != null) perspectiveCamera.enabled = false;
            if (transitionOutputCamera != null) transitionOutputCamera.enabled = false;
            if (blendCanvas != null) blendCanvas.enabled = false;
            if (cameraController != null) cameraController.enabled = true;
        }

        private void OnDestroy()
        {
            Restore2DImmediate();
            if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
            if (twoDTexture != null) { twoDTexture.Release(); Destroy(twoDTexture); }
            if (threeDTexture != null) { threeDTexture.Release(); Destroy(threeDTexture); }
            if (perspectiveCamera != null) Destroy(perspectiveCamera.gameObject);
            if (transitionOutputCamera != null) Destroy(transitionOutputCamera.gameObject);
            if (blendCanvas != null) Destroy(blendCanvas.gameObject);
        }
    }
}
