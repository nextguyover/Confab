namespace Confab.Models.Moderation
{
    public class ModQueueComment
    {
        public string Id {get; set;}
        public DateTime CreationTime {get; set;}
        public string Content {get; set;}
        public DateTime? EditTime {get; set;}
        public string AuthorId {get; set;}
        public string AuthorUsername {get; set;}
        public bool IsAnon {get; set;}
        public string ParentId {get; set;}
        public DateTime? ParentCreationTime {get; set;}
        public string ParentContent {get; set;}
        public string ParentAuthorId {get; set;}
        public string ParentAuthorUsername {get; set;}
        public bool ParentIsAnon {get; set;}
    }
}
