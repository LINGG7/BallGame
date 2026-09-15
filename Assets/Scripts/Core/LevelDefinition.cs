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

		public string LevelTitle => levelTitle;

		public LevelMode Mode => mode;

		public ToolCatalog ToolCatalog => toolCatalog;

		public IReadOnlyList<ToolStock> Tools => tools;
	}
}
