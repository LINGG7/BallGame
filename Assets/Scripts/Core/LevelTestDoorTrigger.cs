using UnityEngine;

namespace BallGame.Core
{
    public enum VisualStageTarget
    {
        To3D,
        To2D
    }

    public sealed class LevelTestDoorTrigger : MonoBehaviour
    {
        [SerializeField] private VisualStageTarget target = VisualStageTarget.To3D;

        private LevelTestVisual3DBootstrap owner;
        private bool used;

        public VisualStageTarget Target => target;

        public void Initialize(LevelTestVisual3DBootstrap value)
        {
            owner = value;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (used || other.GetComponentInParent<BallController>() == null) return;
            used = true;
            if (owner == null) owner = FindObjectOfType<LevelTestVisual3DBootstrap>();
            if (target == VisualStageTarget.To3D)
            {
                owner?.Enter3D(this);
            }
            else
            {
                owner?.Enter2D();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<BallController>() != null)
            {
                used = false;
            }
        }
    }
}
