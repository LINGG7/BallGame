using UnityEngine;

namespace BallGame.Core
{
    public static class LowestCostProgress
    {
        private const string BestCostKeyPrefix = "BallGame.BestCost.";

        public static bool TryGetBestCost(string sceneName, out int bestCost)
        {
            bestCost = 0;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            string key = GetKey(sceneName);
            if (!PlayerPrefs.HasKey(key))
            {
                return false;
            }

            bestCost = Mathf.Max(0, PlayerPrefs.GetInt(key));
            return true;
        }

        public static bool TryRecordBestCost(string sceneName, int currentCost, out int bestCost)
        {
            currentCost = Mathf.Max(0, currentCost);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                bestCost = currentCost;
                return false;
            }

            bool hasBest = TryGetBestCost(sceneName, out bestCost);
            if (hasBest && currentCost >= bestCost)
            {
                return false;
            }

            bestCost = currentCost;
            PlayerPrefs.SetInt(GetKey(sceneName), bestCost);
            PlayerPrefs.Save();
            return true;
        }

        private static string GetKey(string sceneName)
        {
            return BestCostKeyPrefix + sceneName;
        }
    }
}
