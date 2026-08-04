using UnityEngine;

namespace BallGame.Core
{
	public static class ChallengeProgress
	{
		public const int FirstLevel = 1;

		private const string HighestUnlockedLevelKey = "BallGame.Challenge.HighestUnlockedLevel";

		public static int GetHighestUnlockedLevel()
		{
			return Mathf.Max(1, PlayerPrefs.GetInt("BallGame.Challenge.HighestUnlockedLevel", 1));
		}

		public static int GetHighestUnlockedLevel(LevelCatalog catalog)
		{
			int highestUnlockedLevel = GetHighestUnlockedLevel();
			if ((UnityEngine.Object)(object)catalog == (UnityEngine.Object)null || catalog.Count == 0)
			{
				return highestUnlockedLevel;
			}
			return Mathf.Clamp(highestUnlockedLevel, 1, catalog.MaxLevelNumber);
		}

		public static bool IsUnlocked(int levelNumber, LevelCatalog catalog)
		{
			if (levelNumber < 1 || (UnityEngine.Object)(object)catalog == (UnityEngine.Object)null || !catalog.ContainsLevel(levelNumber))
			{
				return false;
			}
			return levelNumber <= GetHighestUnlockedLevel(catalog);
		}

		public static void MarkCompleted(int levelNumber)
		{
			if (levelNumber >= 1)
			{
				int num = levelNumber + 1;
				if (num > GetHighestUnlockedLevel())
				{
					PlayerPrefs.SetInt("BallGame.Challenge.HighestUnlockedLevel", num);
					PlayerPrefs.Save();
				}
			}
		}
	}
}
