namespace BallGame.Core
{
    public sealed class LevelCompletionResult
    {
        public const string DefaultPopupTitle = "闯关成功";

        public string PopupTitle { get; set; } = DefaultPopupTitle;
        public bool HasCostResult { get; set; }
        public int CurrentCost { get; set; }
        public int BestCost { get; set; }
        public bool IsNewBestCost { get; set; }

        public void Reset()
        {
            PopupTitle = DefaultPopupTitle;
            HasCostResult = false;
            CurrentCost = 0;
            BestCost = 0;
            IsNewBestCost = false;
        }
    }
}
