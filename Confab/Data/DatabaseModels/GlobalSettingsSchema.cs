using static Confab.Data.DatabaseModels.CommentLocationSchema;

namespace Confab.Data.DatabaseModels
{
    public class GlobalSettingsSchema
    {
        public int Id { get; set; }

        public CommentingStatus CommentingStatus { get; set; } = CommentingStatus.Enabled;
        public bool VotingEnabled { get; set; } = true;

        public bool AccountCreationEnabled { get; set; } = true;
        public bool AccountLoginEnabled { get; set; } = true;

        public DateTime UserAuthJwtValidityStart { get; set; } = DateTime.UtcNow;

        public bool AdminNotifGlobal { get; set; } = true;
        public bool AdminNotifEditGlobal { get; set; } = true;
        public bool UserNotifGlobal { get; set; } = true;

        public DateTime ModQueueLastCheckedTimestamp { get; set; } = DateTime.UtcNow;
    }
}
