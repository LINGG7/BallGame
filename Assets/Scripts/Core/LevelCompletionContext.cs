using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace BallGame.Core
{
    public sealed class LevelCompletionContext
    {
        private readonly IReadOnlyList<GoalTrigger> goals;
        private readonly IReadOnlyDictionary<GoalTrigger, int> ballCountsByGoal;

        public LevelCompletionContext(IReadOnlyList<GoalTrigger> goals, IReadOnlyDictionary<GoalTrigger, int> ballCountsByGoal, int totalUniqueBallCount)
        {
            SceneName = SceneManager.GetActiveScene().name;
            this.goals = goals;
            this.ballCountsByGoal = ballCountsByGoal;
            TotalUniqueBallCount = totalUniqueBallCount;
        }

        public string SceneName { get; }
        public IReadOnlyList<GoalTrigger> Goals => goals;
        public int TotalUniqueBallCount { get; }

        public int GetBallCount(GoalTrigger goal)
        {
            return goal != null && ballCountsByGoal.TryGetValue(goal, out int count) ? count : 0;
        }
    }
}
