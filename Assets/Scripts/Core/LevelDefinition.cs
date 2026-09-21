using System;
using System.Collections.Generic;
using UnityEngine;

namespace BallGame.Core
{
	[CreateAssetMenu(menuName = "Ball Game/Level Definition")]
	public sealed class LevelDefinition : ScriptableObject
	{
		[Serializable]
		public struct ToolStock
		{
			public ToolDefinition tool;

			public int count;
		}

		[SerializeField]
		private string levelTitle = "Level";

		[SerializeField]
		private LevelMode mode = LevelMode.Normal;

		[SerializeField]
		private ToolCatalog toolCatalog;

		[SerializeField]
		private List<ToolStock> tools = new List<ToolStock>();

		[SerializeField] private List<BallGame.Placement.PlacementHintTarget> placementHints = new List<BallGame.Placement.PlacementHintTarget>();

		public IReadOnlyList<BallGame.Placement.PlacementHintTarget> PlacementHints => placementHints;

        [SerializeField] private List<BallGame.Placement.PlacementHintGroup> placementHintGroups = new List<BallGame.Placement.PlacementHintGroup>();
        public IReadOnlyList<BallGame.Placement.PlacementHintGroup> PlacementHintGroups => placementHintGroups;

        public IEnumerable<BallGame.Placement.PlacementHintGroup> GetEffectivePlacementHintGroups()
        {
            if (placementHintGroups != null && placementHintGroups.Count > 0)
            {
                foreach (var group in placementHintGroups) yield return group;
                yield break;
            }
            if (placementHints == null) yield break;
            for (int i = 0; i < placementHints.Count; i++)
                yield return new BallGame.Placement.PlacementHintGroup
                {
                    groupName = "旧提示 " + (i + 1),
                    targets = new List<BallGame.Placement.PlacementHintTarget> { placementHints[i] }
                };
        }

		public string LevelTitle => levelTitle;

		public LevelMode Mode => mode;

		public ToolCatalog ToolCatalog => toolCatalog;

		public IReadOnlyList<ToolStock> Tools => tools;
	}
}
