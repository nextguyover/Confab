namespace Confab.Models
{
    public class CommentVote
    {
        public string CommentId { get; set; }
        public Vote VoteType { get; set; }
    }

    public enum Vote : short
    {
        None = 0,
        Upvote = 1,
        Downvote = 2,
    }
}
