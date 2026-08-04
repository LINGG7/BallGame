using System;
using UnityEngine;

namespace BallGame.Shop
{
    public interface IRewardedAdService
    {
        void ShowRewardedAd(string placementId, Action<bool> completed);
    }

    public static class BallSkinService
    {
        private const string SelectedKey = "BallGame.Shop.SelectedSkin";
        private const string UnlockedPrefix = "BallGame.Shop.Unlocked.";
        private const string CatalogPath = "Shop/BallSkinCatalog";
        private static BallSkinCatalog catalog;
        private static bool initialized;

        public static event Action<string> SkinChanged;
        public static IRewardedAdService RewardedAdService { get; set; }
        public static BallSkinCatalog Catalog { get { EnsureInitialized(); return catalog; } }
        public static string CurrentSkinId
        {
            get
            {
                EnsureInitialized();
                string fallback = catalog != null ? catalog.DefaultSkinId : "wood";
                return PlayerPrefs.GetString(SelectedKey, fallback);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            catalog = null;
            initialized = false;
            RewardedAdService = null;
            SkinChanged = null;
        }

        public static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            catalog = Resources.Load<BallSkinCatalog>(CatalogPath);
            if (catalog == null) Debug.LogWarning("Ball skin catalog is missing: " + CatalogPath);
            string selected = catalog != null ? catalog.Find(PlayerPrefs.GetString(SelectedKey, catalog.DefaultSkinId))?.id : null;
            if (string.IsNullOrEmpty(selected))
            {
                selected = catalog != null ? catalog.DefaultSkinId : "wood";
                PlayerPrefs.SetString(SelectedKey, selected);
                PlayerPrefs.Save();
            }
        }

        public static bool IsUnlocked(string id)
        {
            EnsureInitialized();
            BallSkinEntry entry = catalog != null ? catalog.Find(id) : null;
            if (entry == null) return false;
            if (entry.initiallyUnlocked || entry.unlockType == BallSkinUnlockType.Free) return true;
            return PlayerPrefs.GetInt(UnlockedPrefix + id, 0) != 0;
        }

        public static void Select(string id)
        {
            EnsureInitialized();
            if (!IsUnlocked(id) || catalog.Find(id) == null) return;
            PlayerPrefs.SetString(SelectedKey, id);
        }

        public static bool EquipSkin(string id)
        {
            EnsureInitialized();
            if (!IsUnlocked(id) || catalog.Find(id) == null) return false;
            PlayerPrefs.SetString(SelectedKey, id);
            PlayerPrefs.Save();
            SkinChanged?.Invoke(id);
            return true;
        }

        public static void TryUnlock(string id, Action<bool> completed = null)
        {
            EnsureInitialized();
            if (IsUnlocked(id)) { completed?.Invoke(true); return; }
            BallSkinEntry entry = catalog != null ? catalog.Find(id) : null;
            if (entry == null) { completed?.Invoke(false); return; }
            if (RewardedAdService == null || entry.unlockType != BallSkinUnlockType.RewardedAd)
            {
                MarkUnlocked(id);
                completed?.Invoke(true);
                return;
            }
            RewardedAdService.ShowRewardedAd(id, success =>
            {
                if (success) MarkUnlocked(id);
                completed?.Invoke(success);
            });
        }

        public static void ApplyTo(SpriteRenderer renderer)
        {
            EnsureInitialized();
            BallSkinEntry entry = catalog != null ? catalog.Find(CurrentSkinId) : null;
            if (renderer != null && entry != null && entry.runtimeSprite != null) renderer.sprite = entry.runtimeSprite;
        }

        private static void MarkUnlocked(string id)
        {
            PlayerPrefs.SetInt(UnlockedPrefix + id, 1);
            PlayerPrefs.Save();
        }
    }
}
