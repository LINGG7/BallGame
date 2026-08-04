using System.Collections.Generic;
using BallGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class WorldTargetIndicator : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform target;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Vector2 edgePadding = new Vector2(42f, 96f);

        private RectTransform indicatorRect;
        private RectTransform canvasRect;
        private Image image;

        private void Awake()
        {
            CacheComponents();
        }

        private void LateUpdate()
        {
            CacheComponents();
            Refresh();
        }

        public void Bind(GameManager manager, Camera camera, Transform targetTransform, Canvas targetCanvas)
        {
            gameManager = manager;
            worldCamera = camera;
            target = targetTransform;
            canvas = targetCanvas;
            CacheComponents();
            Refresh();
        }

        public void BindTargets(GameManager manager, Camera camera, IEnumerable<Transform> targets, Canvas targetCanvas)
        {
            Transform firstTarget = null;
            if (targets != null)
            {
                foreach (Transform candidate in targets)
                {
                    if (candidate != null)
                    {
                        firstTarget = candidate;
                        break;
                    }
                }
            }

            Bind(manager, camera, firstTarget, targetCanvas);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null
                || eventData.button != PointerEventData.InputButton.Left
                || image == null
                || !image.enabled
                || gameManager == null
                || gameManager.State != GameState.Build
                || worldCamera == null
                || target == null)
            {
                return;
            }

            CameraController2D cameraController = worldCamera.GetComponent<CameraController2D>();
            cameraController?.FocusOn(target.position, false);
        }

        private void CacheComponents()
        {
            if (indicatorRect == null)
            {
                indicatorRect = GetComponent<RectTransform>();
            }

            if (image == null)
            {
                image = GetComponent<Image>();
                image.raycastTarget = true;
            }

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (canvasRect == null && canvas != null)
            {
                canvasRect = canvas.transform as RectTransform;
            }
        }

        private void Refresh()
        {
            if (image == null || indicatorRect == null || canvasRect == null || worldCamera == null || target == null || gameManager == null || gameManager.State != GameState.Build)
            {
                SetVisible(false);
                return;
            }

            Vector3 viewport = worldCamera.WorldToViewportPoint(target.position);
            bool inFront = viewport.z > 0f;
            if (IsTargetVisible(inFront))
            {
                SetVisible(false);
                return;
            }

            Vector2 direction = GetCameraToTargetDirection();
            if (!inFront)
            {
                direction = -direction;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            Rect rect = canvasRect.rect;
            float halfWidth = Mathf.Max(1f, rect.width * 0.5f - edgePadding.x);
            float halfHeight = Mathf.Max(1f, rect.height * 0.5f - edgePadding.y);
            float scaleX = Mathf.Abs(direction.x) > 0.0001f ? halfWidth / Mathf.Abs(direction.x) : float.PositiveInfinity;
            float scaleY = Mathf.Abs(direction.y) > 0.0001f ? halfHeight / Mathf.Abs(direction.y) : float.PositiveInfinity;
            Vector2 anchoredPosition = direction * Mathf.Min(scaleX, scaleY);

            indicatorRect.anchoredPosition = anchoredPosition;
            indicatorRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
            SetVisible(true);
        }

        private bool IsTargetVisible(bool inFront)
        {
            if (!inFront)
            {
                return false;
            }

            if (TryGetTargetBounds(out Bounds bounds))
            {
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(worldCamera);
                return GeometryUtility.TestPlanesAABB(planes, bounds);
            }

            Vector3 viewport = worldCamera.WorldToViewportPoint(target.position);
            return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }

        private Vector2 GetCameraToTargetDirection()
        {
            Vector3 cameraPosition = worldCamera.transform.position;
            Vector3 targetPosition = target.position;
            return new Vector2(targetPosition.x - cameraPosition.x, targetPosition.y - cameraPosition.y);
        }

        private bool TryGetTargetBounds(out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            bool hasBounds = false;
            bounds = default;
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null || !targetRenderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
            }

            if (hasBounds)
            {
                return true;
            }

            Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D targetCollider in colliders)
            {
                if (targetCollider == null || !targetCollider.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetCollider.bounds);
                }
            }

            return hasBounds;
        }

        private void SetVisible(bool visible)
        {
            if (image != null && image.enabled != visible)
            {
                image.enabled = visible;
            }
        }
    }
}
