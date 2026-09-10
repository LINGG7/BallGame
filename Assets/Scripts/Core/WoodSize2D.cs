using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BallGame.Core
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class WoodSize2D : MonoBehaviour, ISizeEditable2D
    {
        [SerializeField] private Vector2 size = new Vector2(3f, 0.5f);
        [SerializeField] private Transform visual;
        private BoxCollider2D boxCollider;

        public Vector2 Size => size;

        private void OnEnable() => ApplySize();

        public void SetSize(Vector2 value)
        {
            if (float.IsNaN(value.x) || float.IsInfinity(value.x)
                || float.IsNaN(value.y) || float.IsInfinity(value.y)) return;
            size = new Vector2(Mathf.Max(0.01f, value.x), Mathf.Max(0.01f, value.y));
            ApplySize();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall -= ApplySize;
            EditorApplication.delayCall += ApplySize;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall -= ApplySize;
#endif
        }

        private void ApplySize()
        {
            if (this == null || !isActiveAndEnabled || visual == null) return;
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            // Preserve the registered root scale for level validation and old documents.
            Vector3 rootScale = transform.localScale;
            Vector2 localSize = new Vector2(
                size.x / Mathf.Max(0.0001f, Mathf.Abs(rootScale.x)),
                size.y / Mathf.Max(0.0001f, Mathf.Abs(rootScale.y)));
            Vector3 visualScale = visual.localScale;
            visual.localScale = new Vector3(localSize.x, localSize.y, visualScale.z);
            boxCollider.size = localSize;
        }
    }
}