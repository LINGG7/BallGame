using UnityEngine;

namespace BallGame.Core
{
    public sealed class TrackIntersectionRendererOrder2D : MonoBehaviour
    {
        [SerializeField] private Renderer[] upperRenderers;
        [SerializeField] private Renderer[] lowerRenderers;
        [SerializeField] private int upperOrder = 21;
        [SerializeField] private int lowerOrder;

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            SetOrder(upperRenderers, upperOrder);
            SetOrder(lowerRenderers, lowerOrder);
        }

        private static void SetOrder(Renderer[] renderers, int order)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.sortingOrder = order;
                }
            }
        }
    }
}
