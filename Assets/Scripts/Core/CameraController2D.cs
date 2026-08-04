using BallGame.Placement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BallGame.Core
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController2D : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BallController targetBall;
        [SerializeField] private PlacementManager placementManager;
        [SerializeField] private MapBounds2D mapBounds;
        [SerializeField] private Vector2 followDeadZone = new Vector2(1.8f, 1.1f);
        [SerializeField] private float followSmoothTime = 0.15f;
        [SerializeField] private float buildPanSmoothTime = 0.05f;
        [SerializeField] private float minOrthographicSize = 3f;
        [SerializeField] private float maxOrthographicSize = 5f;
        [SerializeField] private float mouseZoomSpeed = 1f;
        [SerializeField] private float pinchZoomSpeed = 0.05f;
        [SerializeField] private float buildZoomSmoothTime = 0.08f;
        [SerializeField] private float simulationOrthographicSize = 5f;
        [SerializeField] private float simulationZoomSmoothTime = 0.1f;

        private Camera controlledCamera;
        private Vector3 followVelocity;
        private Vector3 panTargetPosition;
        private Vector3 panVelocity;
        private float zoomVelocity;
        private bool isSmoothingSimulationZoom;
        private bool pointerWasDown;
        private bool isPanning;
        private Vector2 lastPanScreenPosition;
        private bool wasPinching;
        private float lastPinchDistance;
        private bool isSubscribedToGameManager;
        private bool isSmoothingBuildZoom;
        private float buildZoomTargetSize;
        private Vector2 buildZoomFocusScreenPosition;
        private readonly List<Transform> simulationTargets = new List<Transform>();

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            controlledCamera.orthographicSize = Mathf.Clamp(controlledCamera.orthographicSize, minOrthographicSize, maxOrthographicSize);
            ClampToBounds();
            panTargetPosition = transform.position;
            SubscribeToGameManager();
        }

        private void OnEnable()
        {
            SubscribeToGameManager();
        }

        private void OnDisable()
        {
            if (gameManager != null && isSubscribedToGameManager)
            {
                gameManager.StateChanged -= OnGameStateChanged;
                isSubscribedToGameManager = false;
            }
        }

        private void OnValidate()
        {
            minOrthographicSize = Mathf.Max(0.01f, minOrthographicSize);
            maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
            mouseZoomSpeed = Mathf.Max(0f, mouseZoomSpeed);
            pinchZoomSpeed = Mathf.Max(0f, pinchZoomSpeed);
            buildZoomSmoothTime = Mathf.Max(0f, buildZoomSmoothTime);
            simulationOrthographicSize = Mathf.Clamp(simulationOrthographicSize, minOrthographicSize, maxOrthographicSize);
            simulationZoomSmoothTime = Mathf.Max(0f, simulationZoomSmoothTime);
        }

        public void FocusOnBall(bool immediate)
        {
            if (targetBall != null)
            {
                FocusOn(targetBall.transform.position, immediate);
            }
        }

        public void SetSimulationTargets(IEnumerable<Transform> targets)
        {
            simulationTargets.Clear();
            if (targets == null)
            {
                return;
            }

            foreach (Transform target in targets)
            {
                if (target != null)
                {
                    simulationTargets.Add(target);
                }
            }
        }

        public void ClearSimulationTargets()
        {
            simulationTargets.Clear();
        }

        public void FocusOn(Vector3 worldPosition, bool immediate)
        {
            Vector3 targetPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            targetPosition = ClampPosition(targetPosition);
            panTargetPosition = targetPosition;
            followVelocity = Vector3.zero;
            panVelocity = Vector3.zero;

            if (immediate)
            {
                transform.position = targetPosition;
            }
        }

        private void Update()
        {
            if (gameManager == null || gameManager.State != GameState.Build)
            {
                ResetPanState();
                return;
            }

            if (HandleBuildZoom())
            {
                ResetPanState(false);
                return;
            }

            HandleBuildPan();
        }

        private void LateUpdate()
        {
            if (gameManager != null && gameManager.State == GameState.Build)
            {
                ApplyBuildZoomDamping();
                ApplyBuildPanDamping();
            }
            else if (gameManager != null && gameManager.State == GameState.Simulate)
            {
                ApplySimulationZoom();
                FollowTargetWithDeadZone();
            }

            ClampToBounds();
        }

        private void HandleBuildPan()
        {
            if (Input.touchCount > 1)
            {
                ResetPanState(false);
                return;
            }

            bool pointerDown = TryGetPointer(out Vector2 screenPosition, out bool justPressed, out bool justReleased, out int pointerId);
            bool pointerOverUi = IsPointerOverUi(pointerId);

            if (justPressed)
            {
                isPanning = !pointerOverUi && !IsPointerOverPlaceable(screenPosition);
                lastPanScreenPosition = screenPosition;
                panTargetPosition = transform.position;
                panVelocity = Vector3.zero;
            }

            if (isPanning && pointerDown)
            {
                Vector2 screenDelta = screenPosition - lastPanScreenPosition;
                PanByScreenDelta(screenDelta);
                lastPanScreenPosition = screenPosition;
            }

            if (justReleased)
            {
                isPanning = false;
            }

            pointerWasDown = pointerDown;
        }

        private bool HandleBuildZoom()
        {
            if (controlledCamera == null)
            {
                return false;
            }

            if (Input.touchCount >= 2)
            {
                return HandlePinchZoom();
            }

            wasPinching = false;
            return HandleMouseWheelZoom();
        }

        private bool HandleMouseWheelZoom()
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scrollDelta, 0f) || IsPointerOverUi(-1))
            {
                return false;
            }

            float targetSize = controlledCamera.orthographicSize - scrollDelta * mouseZoomSpeed;
            SetBuildZoomTarget(targetSize, Input.mousePosition);
            return true;
        }

        private void ApplySimulationZoom()
        {
            if (!isSmoothingSimulationZoom || controlledCamera == null)
            {
                return;
            }

            float targetSize = Mathf.Clamp(simulationOrthographicSize, minOrthographicSize, maxOrthographicSize);
            if (simulationZoomSmoothTime <= 0f)
            {
                controlledCamera.orthographicSize = targetSize;
                isSmoothingSimulationZoom = false;
                zoomVelocity = 0f;
                return;
            }

            controlledCamera.orthographicSize = Mathf.SmoothDamp(controlledCamera.orthographicSize, targetSize, ref zoomVelocity, simulationZoomSmoothTime);
            if (Mathf.Abs(controlledCamera.orthographicSize - targetSize) <= 0.001f)
            {
                controlledCamera.orthographicSize = targetSize;
                isSmoothingSimulationZoom = false;
                zoomVelocity = 0f;
            }
        }

        private bool HandlePinchZoom()
        {
            Touch firstTouch = Input.GetTouch(0);
            Touch secondTouch = Input.GetTouch(1);

            if (IsPointerOverUi(firstTouch.fingerId) || IsPointerOverUi(secondTouch.fingerId))
            {
                wasPinching = false;
                return false;
            }

            float currentDistance = Vector2.Distance(firstTouch.position, secondTouch.position);
            Vector2 center = (firstTouch.position + secondTouch.position) * 0.5f;

            if (!wasPinching)
            {
                wasPinching = true;
                lastPinchDistance = currentDistance;
                return true;
            }

            float distanceDelta = currentDistance - lastPinchDistance;
            lastPinchDistance = currentDistance;

            if (Mathf.Approximately(distanceDelta, 0f))
            {
                return true;
            }

            float targetSize = controlledCamera.orthographicSize - distanceDelta * pinchZoomSpeed;
            SetBuildZoomTarget(targetSize, center);
            return true;
        }

        private void SetBuildZoomTarget(float targetSize, Vector2 focusScreenPosition)
        {
            buildZoomTargetSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);
            buildZoomFocusScreenPosition = focusScreenPosition;
            isSmoothingBuildZoom = true;
        }

        private void ApplyBuildZoomDamping()
        {
            if (!isSmoothingBuildZoom || controlledCamera == null)
            {
                return;
            }

            if (buildZoomSmoothTime <= 0f)
            {
                ZoomToSizeImmediately(buildZoomTargetSize, buildZoomFocusScreenPosition);
                isSmoothingBuildZoom = false;
                zoomVelocity = 0f;
                return;
            }

            float nextSize = Mathf.SmoothDamp(controlledCamera.orthographicSize, buildZoomTargetSize, ref zoomVelocity, buildZoomSmoothTime);
            ZoomToSizeImmediately(nextSize, buildZoomFocusScreenPosition);

            if (Mathf.Abs(controlledCamera.orthographicSize - buildZoomTargetSize) <= 0.001f)
            {
                ZoomToSizeImmediately(buildZoomTargetSize, buildZoomFocusScreenPosition);
                isSmoothingBuildZoom = false;
                zoomVelocity = 0f;
            }
        }

        private void ZoomToSizeImmediately(float targetSize, Vector2 focusScreenPosition)
        {
            float clampedSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);
            if (!Mathf.Approximately(controlledCamera.orthographicSize, clampedSize))
            {
                Vector3 focusBefore = ScreenToWorldPointOnCameraPlane(focusScreenPosition);
                controlledCamera.orthographicSize = clampedSize;
                Vector3 focusAfter = ScreenToWorldPointOnCameraPlane(focusScreenPosition);
                Vector3 compensatedPosition = transform.position + (focusBefore - focusAfter);
                transform.position = ClampPosition(compensatedPosition);
            }

            panTargetPosition = transform.position;
            panVelocity = Vector3.zero;
        }

        private void PanByScreenDelta(Vector2 screenDelta)
        {
            if (controlledCamera == null || screenDelta.sqrMagnitude <= 0f)
            {
                return;
            }

            float unitsPerPixel = (controlledCamera.orthographicSize * 2f) / Mathf.Max(1, controlledCamera.pixelHeight);
            Vector3 worldDelta = new Vector3(screenDelta.x * unitsPerPixel, screenDelta.y * unitsPerPixel, 0f);
            panTargetPosition = ClampPosition(panTargetPosition - worldDelta);
        }

        private void ApplyBuildPanDamping()
        {
            panTargetPosition = ClampPosition(panTargetPosition);
            if (buildPanSmoothTime <= 0f)
            {
                transform.position = panTargetPosition;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, panTargetPosition, ref panVelocity, buildPanSmoothTime);
        }

        private void FollowTargetWithDeadZone()
        {
            if (!TryGetSimulationTargetPosition(out Vector3 ballPosition))
            {
                return;
            }

            Vector3 current = transform.position;
            Vector3 target = current;
            Vector2 delta = ballPosition - current;

            if (delta.x > followDeadZone.x)
            {
                target.x = ballPosition.x - followDeadZone.x;
            }
            else if (delta.x < -followDeadZone.x)
            {
                target.x = ballPosition.x + followDeadZone.x;
            }

            if (delta.y > followDeadZone.y)
            {
                target.y = ballPosition.y - followDeadZone.y;
            }
            else if (delta.y < -followDeadZone.y)
            {
                target.y = ballPosition.y + followDeadZone.y;
            }

            target.z = current.z;
            target = ClampPosition(target);
            transform.position = Vector3.SmoothDamp(current, target, ref followVelocity, followSmoothTime);
        }

        private bool TryGetSimulationTargetPosition(out Vector3 position)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = simulationTargets.Count - 1; i >= 0; i--)
            {
                Transform target = simulationTargets[i];
                if (target == null)
                {
                    simulationTargets.RemoveAt(i);
                    continue;
                }

                sum += target.position;
                count++;
            }

            if (count > 0)
            {
                position = sum / count;
                return true;
            }

            if (targetBall != null)
            {
                position = targetBall.transform.position;
                return true;
            }

            position = transform.position;
            return false;
        }

        private bool IsPointerOverPlaceable(Vector2 screenPosition)
        {
            return placementManager != null && placementManager.TryGetPlaceableAtScreenPosition(screenPosition, out _);
        }

        private bool TryGetPointer(out Vector2 screenPosition, out bool justPressed, out bool justReleased, out int pointerId)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                justPressed = touch.phase == TouchPhase.Began;
                justReleased = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            }

            screenPosition = Input.mousePosition;
            pointerId = -1;
            bool down = Input.GetMouseButton(0);
            justPressed = !pointerWasDown && down;
            justReleased = pointerWasDown && !down;
            return down;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private Vector3 ScreenToWorldPointOnCameraPlane(Vector2 screenPosition)
        {
            float depth = Mathf.Abs(transform.position.z);
            return controlledCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Simulate)
            {
                isSmoothingBuildZoom = false;
                isSmoothingSimulationZoom = true;
                zoomVelocity = 0f;
            }
            else
            {
                isSmoothingBuildZoom = false;
                isSmoothingSimulationZoom = false;
                zoomVelocity = 0f;
            }
        }

        private void SubscribeToGameManager()
        {
            if (gameManager == null || isSubscribedToGameManager)
            {
                return;
            }

            gameManager.StateChanged += OnGameStateChanged;
            isSubscribedToGameManager = true;
        }

        private void ResetPanState(bool resetPinch = true)
        {
            isPanning = false;
            if (resetPinch)
            {
                wasPinching = false;
                lastPinchDistance = 0f;
            }

            pointerWasDown = Input.GetMouseButton(0) || Input.touchCount > 0;
            panTargetPosition = transform.position;
            panVelocity = Vector3.zero;
        }

        private void ClampToBounds()
        {
            transform.position = ClampPosition(transform.position);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            return mapBounds != null ? mapBounds.ClampCameraPosition(controlledCamera, position) : position;
        }
    }
}
