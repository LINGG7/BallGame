using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BallGame.Core
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ObstacleSize2D : MonoBehaviour
    {
        [SerializeField] private Vector2 size = Vector2.one;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;

        public Vector2 Size => size;

        private void Awake()
        {
            CacheComponents();
            ApplySize();
        }

        private void OnValidate()
        {
            size = new Vector2(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
#if UNITY_EDITOR
            EditorApplication.delayCall -= ApplySizeAfterValidation;
            EditorApplication.delayCall += ApplySizeAfterValidation;
#else
            CacheComponents();
            ApplySize();
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall -= ApplySizeAfterValidation;
#endif
        }

#if UNITY_EDITOR
        private void ApplySizeAfterValidation()
        {
            EditorApplication.delayCall -= ApplySizeAfterValidation;
            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            CacheComponents();
            ApplySize();
        }
#endif

        public void SetSize(Vector2 newSize)
        {
            size = new Vector2(Mathf.Max(0.01f, newSize.x), Mathf.Max(0.01f, newSize.y));
            ApplySize();
        }

        private void CacheComponents()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
            }
        }

        private void ApplySize()
        {
            Vector3 scale = transform.localScale;
            if (!Mathf.Approximately(scale.x, 1f) || !Mathf.Approximately(scale.y, 1f))
            {
                size = new Vector2(Mathf.Max(0.01f, size.x * Mathf.Abs(scale.x)), Mathf.Max(0.01f, size.y * Mathf.Abs(scale.y)));
                transform.localScale = new Vector3(Mathf.Sign(scale.x == 0f ? 1f : scale.x), Mathf.Sign(scale.y == 0f ? 1f : scale.y), scale.z);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.drawMode = SpriteDrawMode.Sliced;
                spriteRenderer.size = size;
            }

            if (boxCollider != null)
            {
                boxCollider.offset = Vector2.zero;
                boxCollider.size = size;
            }
        }
    }
}
