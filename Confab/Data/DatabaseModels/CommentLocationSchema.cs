using System.ComponentModel.DataAnnotations.Schema;

namespace Confab.Data.DatabaseModels
{
    public class CommentLocationSchema
    {
        public int Id { get; set; }
        public string LocationStr {  get; set; }

        public CommentingStatus LocalStatus { get; set; }
        public bool LocalVotingEnabled { get; set; } = true;
        public bool LocalEditingEnabled { get; set; } = true;

        public bool AdminNotifLocal { get; set; } = true;
        public bool AdminNotifEditLocal { get; set; } = true;
        public bool UserNotifLocal { get; set; } = true;

        [InverseProperty("Location")]
        public List<CommentSchema> Comments { get; set; } = new List<CommentSchema>();

        public enum CommentingStatus : short
        {
            Enabled = 0,
            Locked = 1,
            Hidden = 2,
        }
    }
}
