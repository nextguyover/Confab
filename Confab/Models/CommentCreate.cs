namespace Confab.Models
{
    public class CommentCreate
    {
        public string Location { get; set; }
        public string Content { get; set; }
        //public string UserEmail { get; set; }
        public string ParentCommentId { get; set; }
    }
}
