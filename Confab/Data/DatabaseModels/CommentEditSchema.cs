using System.ComponentModel.DataAnnotations.Schema;

namespace Confab.Data.DatabaseModels
{
    public class CommentEditSchema
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime VisibilityStartTime { get; set; }
        public CommentSchema SourceComment { get; set; }
    }
}
