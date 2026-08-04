using UnityEngine;

namespace BallGame.Core
{
    public abstract class LevelEndingRule : MonoBehaviour
    {
        public abstract void Evaluate(LevelCompletionContext context, LevelCompletionResult result);
    }
}
