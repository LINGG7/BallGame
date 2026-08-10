using System;
using System.Collections.Generic;
using BallGame.Core;
using UnityEngine;

namespace BallGame.LevelDesign
{
    [Serializable]
    public sealed class CollaborativeLevelAssetEntry
    {
        public string assetKey;
        public CollaborativeObjectKind kind;
        public GameObject prefab;
        public Sprite icon;
        public ToolDefinition toolDefinition;
        public bool supportsSpline;

        public bool IsInventoryTool => kind == CollaborativeObjectKind.Tool && toolDefinition != null;
    }

    [CreateAssetMenu(menuName = "Ball Game/Level Design/Asset Registry", fileName = "CollaborativeLevelAssetRegistry")]
    public sealed class CollaborativeLevelAssetRegistry : ScriptableObject
    {
        [SerializeField] private List<CollaborativeLevelAssetEntry> entries = new List<CollaborativeLevelAssetEntry>();

        public IReadOnlyList<CollaborativeLevelAssetEntry> Entries => entries;

        public CollaborativeLevelAssetEntry Find(string assetKey)
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && string.Equals(entries[i].assetKey, assetKey, StringComparison.Ordinal))
                    return entries[i];
            }
            return null;
        }
    }
}
