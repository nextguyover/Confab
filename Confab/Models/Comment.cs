namespace Confab.Models
{
    public class Comment
    {
        public string CommentId { get; set; }
        public string? Content { get; set; } = null;
        public DateTime? CreationTime { get; set; } = null;
        public bool? CommentEdited { get; set; } = null;
        public bool? CanEdit { get; set; } = null;
        public DateTime? EditTime { get; set; } = null;
        public bool? EditHistoryAvailable { get; set; } = null;
        public bool? IsDeleted { get; set; } = null;
        public bool? IsBanned { get; set; } = null;
        public bool? AwaitingModeration { get; set; } = null;

        public string AuthorUsername { get; set; } = null;
        public string AuthorId { get; set; } = null;
        public bool? IsAdmin { get; set; } = null;
        public bool? IsAuthor { get; set; } = null;

        public int? Upvotes { get; set; } = null;
        public int? Downvotes { get; set; } = null;

        public Vote? UserVote { get; set; } = null;

        public List<Comment> ChildComments { get; set; } = new List<Comment>();
    }
}
