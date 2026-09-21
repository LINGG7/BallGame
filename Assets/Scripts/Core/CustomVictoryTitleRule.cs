using UnityEngine;

namespace BallGame.Core
{
    [DisallowMultipleComponent]
    public sealed class CustomVictoryTitleRule : LevelEndingRule
    {
        [SerializeField, TextArea(1, 3)]
        [Tooltip("通关时显示的彩蛋标题；留空则保留原有标题。")]
        private string victoryTitle = "";

        public override void Evaluate(LevelCompletionContext context, LevelCompletionResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(victoryTitle))
            {
                return;
            }

            result.PopupTitle = victoryTitle;
        }
    }
}
