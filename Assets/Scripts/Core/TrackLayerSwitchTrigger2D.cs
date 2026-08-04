using UnityEngine;

namespace BallGame.Core
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class TrackLayerSwitchTrigger2D : MonoBehaviour
    {
        [SerializeField] private TrackLayerSwitcher2D switcher;
        [SerializeField] private bool useUpper = true;

        private void Awake()
        {
            EnsureTrigger();
        }

        private void OnValidate()
        {
            EnsureTrigger();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (switcher == null || other.GetComponentInParent<BallController>() == null)
            {
                return;
            }

            switcher.SetLayer(useUpper);
        }

        private void EnsureTrigger()
        {
            Collider2D triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }
    }
}
