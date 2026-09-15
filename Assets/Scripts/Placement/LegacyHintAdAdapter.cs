using System;
using BallGame.Shop;

namespace BallGame.Placement
{
    // Existing shop providers only expose a bool, so a negative result maps to Failed.
    public sealed class LegacyHintAdAdapter : HintRewardedAdProvider
    {
        public override bool IsAvailable => BallSkinService.RewardedAdService != null;
        public override void Show(Action<HintAdResult> completed)
        {
            var service = BallSkinService.RewardedAdService;
            if (service == null) { completed(HintAdResult.Failed); return; }
            service.ShowRewardedAd("placement_hint", rewarded =>
                completed(rewarded ? HintAdResult.Rewarded : HintAdResult.Failed));
        }
    }
}