using System.Collections.Generic;
using UnityEngine;

namespace BallGame.Core
{
    public sealed class Level10GoalDistributionEndingRule : LevelEndingRule
    {
        [SerializeField] private string normalEggTitle = "普通彩蛋结局";
        [SerializeField] private string specialEggTitle = "特殊彩蛋结局";
        [SerializeField] private GameObject firework;

        public override void Evaluate(LevelCompletionContext context, LevelCompletionResult result)
        {
            if (context == null || result == null || context.Goals == null || context.Goals.Count != 2)
            {
                return;
            }

            List<int> counts = new List<int>(2);
            for (int i = 0; i < context.Goals.Count; i++)
            {
                counts.Add(context.GetBallCount(context.Goals[i]));
            }

            counts.Sort();
            if (counts[0] == 0 && counts[1] == 2)
            {
                result.PopupTitle = specialEggTitle;
                if (firework != null)
                {
                    firework.SetActive(true);
                }
            }
            else if (counts[0] == 1 && counts[1] == 1)
            {
                result.PopupTitle = normalEggTitle;
            }
        }
    }
}
