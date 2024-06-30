namespace Confab.Models
{
    public class CommentHistoryItem
    {
        public string Content { get; set; }
        public DateTime VisibilityStartTime { get; set; }
        public bool? BeforeModeratorApproval { get; set; }
    }
}
