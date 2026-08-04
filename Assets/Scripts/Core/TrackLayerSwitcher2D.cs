using UnityEngine;

namespace BallGame.Core
{
    public sealed class TrackLayerSwitcher2D : MonoBehaviour
    {
        [SerializeField] private Collider2D[] upperColliders;
        [SerializeField] private Collider2D[] lowerColliders;
        [SerializeField] private bool defaultUseUpper = true;

        public bool IsUpperActive { get; private set; }

        private void OnEnable()
        {
            SetLayer(defaultUseUpper);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                SetLayer(defaultUseUpper);
            }
        }

        public void SetUpperActive()
        {
            SetLayer(true);
        }

        public void SetLowerActive()
        {
            SetLayer(false);
        }

        public void SetLayer(bool useUpper)
        {
            IsUpperActive = useUpper;
            SetCollidersEnabled(upperColliders, useUpper);
            SetCollidersEnabled(lowerColliders, !useUpper);
        }

        private static void SetCollidersEnabled(Collider2D[] colliders, bool enabled)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }
    }
}
