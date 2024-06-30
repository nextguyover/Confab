using static Confab.Data.DatabaseModels.CommentLocationSchema;
using static Confab.Data.DatabaseModels.GlobalSettingsSchema;

namespace Confab.Models.AdminPanel.CommentSettings
{
    public class GlobalCommentSettings
    {
        public CommentingStatus? CommentingStatus { get; set; }
        public bool? VotingEnabled { get; set; }
        public bool? AccountCreationEnabled { get; set; }
        public bool? AccountLoginEnabled { get; set; }
    }
}
