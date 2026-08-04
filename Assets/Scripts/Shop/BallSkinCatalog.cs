using System;
using System.Collections.Generic;
using UnityEngine;

namespace BallGame.Shop
{
    public enum BallSkinUnlockType
    {
        Free,
        RewardedAd
    }

    [Serializable]
    public sealed class BallSkinEntry
    {
        public string id;
        public string displayName;
        public Sprite previewSprite;
        public Sprite runtimeSprite;
        public bool featured;
        public bool initiallyUnlocked;
        public BallSkinUnlockType unlockType;
    }

    [CreateAssetMenu(menuName = "Ball Game/Shop/Ball Skin Catalog", fileName = "BallSkinCatalog")]
    public sealed class BallSkinCatalog : ScriptableObject
    {
        [SerializeField] private string defaultSkinId = "wood";
        [SerializeField] private List<BallSkinEntry> skins = new List<BallSkinEntry>();

        public string DefaultSkinId => defaultSkinId;
        public IReadOnlyList<BallSkinEntry> Skins => skins;

        public BallSkinEntry Find(string id)
        {
            for (int i = 0; i < skins.Count; i++)
            {
                if (skins[i] != null && string.Equals(skins[i].id, id, StringComparison.Ordinal))
                    return skins[i];
            }
            return null;
        }
    }
}
