namespace Confab.Models
{
    public class CommentGetAtLocation
    {
        public string Location { get; set; }
        public CommentSort Sort { get; set; } = CommentSort.Upvotes;
    }

    public enum CommentSort : short
    {
        Upvotes = 0,
        Downvotes = 1,
        Newest = 2,
        Oldest = 3,
    }
}
