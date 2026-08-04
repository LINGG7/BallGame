using UnityEngine;

namespace BallGame.Core
{
    public static class LevelProgress
    {
        public const int FirstLevel = 1;

        private const string HighestUnlockedLevelKey = "BallGame.HighestUnlockedLevel";

        public static int GetHighestUnlockedLevel()
        {
            return Mathf.Max(FirstLevel, PlayerPrefs.GetInt(HighestUnlockedLevelKey, FirstLevel));
        }

        public static int GetHighestUnlockedLevel(LevelCatalog catalog)
        {
            int highestUnlocked = GetHighestUnlockedLevel();
            if (catalog == null || catalog.Count == 0)
            {
                return highestUnlocked;
            }

            return Mathf.Clamp(highestUnlocked, FirstLevel, catalog.MaxLevelNumber);
        }

        public static bool IsUnlocked(int levelNumber)
        {
            return levelNumber >= FirstLevel && levelNumber <= GetHighestUnlockedLevel();
        }

        public static bool IsUnlocked(int levelNumber, LevelCatalog catalog)
        {
            if (catalog != null && !catalog.ContainsLevel(levelNumber))
            {
                return false;
            }

            return levelNumber >= FirstLevel && levelNumber <= GetHighestUnlockedLevel(catalog);
        }

        public static void MarkCompleted(int levelNumber)
        {
            MarkCompleted(levelNumber, null);
        }

        public static void MarkCompleted(int levelNumber, LevelCatalog catalog)
        {
            if (levelNumber < FirstLevel)
            {
                return;
            }

            if (catalog != null && !catalog.ContainsLevel(levelNumber))
            {
                return;
            }

            int highestUnlocked = GetHighestUnlockedLevel();
            int nextUnlocked = levelNumber + 1;
            if (catalog != null && catalog.Count > 0)
            {
                nextUnlocked = Mathf.Min(nextUnlocked, catalog.MaxLevelNumber);
            }

            if (nextUnlocked > highestUnlocked)
            {
                PlayerPrefs.SetInt(HighestUnlockedLevelKey, nextUnlocked);
                PlayerPrefs.Save();
            }
        }
    }
}
