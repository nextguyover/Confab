namespace Confab.Data.DatabaseModels
{
    public class AutoModerationRuleSchema
    {
        public int Id { get; set; }
        public string FilterRegex { get; set; }
        public string ReturnError { get; set; }
        public AutoModerationAction MatchAction { get; set; }
        public bool NotifyAdmins { get; set; }

        public enum AutoModerationAction : short
        {
            BlockPosting = 0,
            Ban = 1,
            BanAndDeleteAll = 2,
            Notify = 3,
            SendToModQueue = 4,
        }
    }
}
