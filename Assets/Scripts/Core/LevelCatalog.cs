using System;
using System.Collections.Generic;
using UnityEngine;

namespace BallGame.Core
{
	[CreateAssetMenu(menuName = "Ball Game/Level Catalog")]
	public sealed class LevelCatalog : ScriptableObject
	{
		[Serializable]
		public sealed class LevelEntry
		{
			[SerializeField]
			private int levelNumber = 1;

			[SerializeField]
			private string sceneName = "Level_01";

			[SerializeField]
			private string displayTitle = "01 Level";

			[SerializeField]
			private LevelDefinition levelDefinition;

			public int LevelNumber => levelNumber;

			public string SceneName => sceneName;

			public string DisplayTitle => displayTitle;

			public LevelDefinition LevelDefinition => levelDefinition;
		}

		[SerializeField]
		private List<LevelEntry> levels = new List<LevelEntry>();

		public IReadOnlyList<LevelEntry> Levels => levels;

		public int Count => (levels != null) ? levels.Count : 0;

		public int MaxLevelNumber
		{
			get
			{
				int num = 1;
				if (levels == null)
				{
					return num;
				}
				foreach (LevelEntry level in levels)
				{
					if (level != null && level.LevelNumber > num)
					{
						num = level.LevelNumber;
					}
				}
				return num;
			}
		}

		public bool ContainsLevel(int levelNumber)
		{
			return GetLevel(levelNumber) != null;
		}

		public LevelEntry GetLevel(int levelNumber)
		{
			if (levels == null)
			{
				return null;
			}
			foreach (LevelEntry level in levels)
			{
				if (level != null && level.LevelNumber == levelNumber)
				{
					return level;
				}
			}
			return null;
		}
	}
}
