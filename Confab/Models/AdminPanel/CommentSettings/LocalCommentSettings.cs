using static Confab.Data.DatabaseModels.CommentLocationSchema;

namespace Confab.Models.AdminPanel.CommentSettings
{
    public class LocalCommentSettings
    {
        public CommentingStatusResponse CommentingStatus { get; set; }
        public bool? VotingStatus { get; set; }
        public bool? EditingStatus { get; set; }

        public enum CommentingStatusResponse : short
        {
            Enabled = 0,
            Locked = 1,
            Hidden = 2,
            Uninitialised = 3,
        }
    }
}
