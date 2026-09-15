using System;
using UnityEngine;

namespace BallGame.Placement
{
    public enum HintAdResult { Rewarded, Cancelled, Failed }

    // SDK implementations must invoke completion on Unity's main thread, after reward confirmation.
    public abstract class HintRewardedAdProvider : MonoBehaviour
    {
        public abstract bool IsAvailable { get; }
        public abstract void Show(Action<HintAdResult> completed);
        public virtual void CancelPending() { }
    }

}
