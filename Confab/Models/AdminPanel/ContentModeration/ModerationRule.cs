using static Confab.Data.DatabaseModels.AutoModerationRuleSchema;

namespace Confab.Models.AdminPanel.ContentModeration
{
    public class ModerationRule
    {
        public string FilterRegex { get; set; }
        public AutoModerationAction MatchAction { get; set; }
        public string ReturnError { get; set; }
        public bool NotifyAdmins { get; set; }
    }
}
