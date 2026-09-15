using System;
using UnityEngine;

namespace BallGame.Placement
{
    public sealed class MockHintAdProvider : HintRewardedAdProvider
    {
        [Tooltip("Manual: use the component context menu to return a result during Play Mode.")]
        [SerializeField] private bool manualCompletion;
        [SerializeField] private HintAdResult result = HintAdResult.Rewarded;
        [SerializeField, Min(0f)] private float delay = 1f;
        private Action<HintAdResult> pending;
        private float finishAt;
        public override bool IsAvailable
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return isActiveAndEnabled && pending == null;
#else
                return false;
#endif
            }
        }
        public override void Show(Action<HintAdResult> completed)
        {
            if (!IsAvailable) { completed?.Invoke(HintAdResult.Failed); return; }
            pending = completed;
            finishAt = Time.unscaledTime + Mathf.Max(0f, delay);
        }
        private void Update()
        {
            if (!manualCompletion && pending != null && Time.unscaledTime >= finishAt) Complete(result);
        }
        [ContextMenu("Complete Mock / Rewarded")]
        public void CompleteRewarded() => Complete(HintAdResult.Rewarded);
        [ContextMenu("Complete Mock / Cancelled")]
        public void CompleteCancelled() => Complete(HintAdResult.Cancelled);
        [ContextMenu("Complete Mock / Failed")]
        public void CompleteFailed() => Complete(HintAdResult.Failed);
        private void Complete(HintAdResult outcome)
        {
            var callback = pending;
            pending = null;
            callback?.Invoke(outcome);
        }
        public override void CancelPending() => pending = null;
        private void OnDisable() => Complete(HintAdResult.Failed);
    }
}