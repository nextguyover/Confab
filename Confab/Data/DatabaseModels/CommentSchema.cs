using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Confab.Data.DatabaseModels
{
    [Index(propertyNames: nameof(PublicId), IsUnique = true)]
    public class CommentSchema
    {
        public int Id { get; set; }
        public string PublicId { get; set; }
        public CommentLocationSchema Location { get; set; }
        public string Content { get; set; }
        public DateTime CreationTime  { get; set; }
        public DateTime EditTime { get; set; }
        public bool IsDeleted { get; set; }
        public bool AwaitingModeration { get; set; }
        public DateTime ModeratorApprovalTimestamp { get; set; }
        public UserSchema Author { get; set; }

        public List<CommentSchema> ChildComments { get; set; } = new List<CommentSchema>();
        public CommentSchema ParentComment { get; set; }


        [InverseProperty("UpvotedComments")]
        public List<UserSchema> UpvotedUsers { get; set; } = new List<UserSchema>();

        [InverseProperty("DownvotedComments")]
        public List<UserSchema> DownvotedUsers { get; set; } = new List<UserSchema>();

        [InverseProperty("SourceComment")]
        public List<CommentEditSchema> CommentEdits { get; set; } = new List<CommentEditSchema>();
    }
}
