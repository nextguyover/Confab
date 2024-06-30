using static Confab.Models.AdminPanel.CommentSettings.LocalCommentSettings;
using static Confab.Data.DatabaseModels.CommentLocationSchema;

namespace Confab.Models.AdminPanel.CommentSettings
{
    public class SetLocalCommentSettings
    {
        public string Location { get; set; }
        public CommentingStatus? CommentingStatus { get; set; }
        public bool? VotingStatus { get; set; }
        public bool? EditingStatus { get; set; }
    }
}
