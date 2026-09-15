using System;
using UnityEngine;

namespace BallGame.Shop
{
    /// <summary>Independent ownership and equipment for ball trail effects.</summary>
    public static class EffectSkinService
    {
        private const string SelectedKey = "BallGame.Shop.SelectedEffect";
        private const string UnlockedPrefix = "BallGame.Shop.UnlockedEffect.";
        private static BallSkinCatalog catalog;
        private static bool requestPending;
        public static event Action<string> SkinChanged;
        public static bool IsUnlockPending => requestPending;
        public static BallSkinCatalog Catalog => catalog != null ? catalog : (catalog = Resources.Load<BallSkinCatalog>("Shop/EffectSkinCatalog"));
        public static string CurrentSkinId
        {
            get
            {
                var currentCatalog = Catalog;
                if (currentCatalog == null) return "basic";
                string selected = PlayerPrefs.GetString(SelectedKey, currentCatalog.DefaultSkinId);
                return IsUnlocked(selected) ? selected : currentCatalog.DefaultSkinId;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            catalog = null;
            requestPending = false;
            SkinChanged = null;
        }

        public static bool IsUnlocked(string id)
        {
            var entry = Catalog != null ? Catalog.Find(id) : null;
            return entry != null && (entry.initiallyUnlocked || entry.unlockType == BallSkinUnlockType.Free
                || PlayerPrefs.GetInt(UnlockedPrefix + id, 0) == 1);
        }

        public static bool EquipSkin(string id)
        {
            if (!IsUnlocked(id)) return false;
            PlayerPrefs.SetString(SelectedKey, id);
            PlayerPrefs.Save();
            SkinChanged?.Invoke(id);
            return true;
        }

        public static void TryUnlock(string id, Action<bool> completed = null)
        {
            if (IsUnlocked(id)) { completed?.Invoke(true); return; }
            var entry = Catalog != null ? Catalog.Find(id) : null;
            var ads = BallSkinService.RewardedAdService;
            if (entry == null || entry.unlockType != BallSkinUnlockType.RewardedAd || ads == null || requestPending)
            {
                completed?.Invoke(false);
                return;
            }

            requestPending = true;
            bool finished = false;
            Action<bool> finish = success =>
            {
                // Some SDKs invoke both close and reward callbacks; consume completion once.
                if (finished) return;
                finished = true;
                requestPending = false;
                if (success)
                {
                    PlayerPrefs.SetInt(UnlockedPrefix + id, 1);
                    PlayerPrefs.Save();
                }
                completed?.Invoke(success);
            };
            try { ads.ShowRewardedAd("effect_" + id, finish); }
            catch (Exception exception)
            {
                Debug.LogWarning("Effect rewarded ad failed: " + exception.Message);
                finish(false);
            }
        }
    }
}