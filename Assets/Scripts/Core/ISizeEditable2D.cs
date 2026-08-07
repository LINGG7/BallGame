using UnityEngine;

namespace BallGame.Core
{
    public interface ISizeEditable2D
    {
        Vector2 Size { get; }
        void SetSize(Vector2 value);
    }

    public static class SizeEditable2DUtility
    {
        public static bool TryGet(GameObject target, out ISizeEditable2D editable)
        {
            editable = null;
            if (target == null) return false;
            MonoBehaviour[] behaviours = target.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (!(behaviours[i] is ISizeEditable2D candidate)) continue;
                editable = candidate;
                return true;
            }
            return false;
        }
    }
}
