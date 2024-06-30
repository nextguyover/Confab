using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public abstract class CommentNotificationsTemplatingScaffold : AllEmailsTemplatingScaffold
    {
        private string _CommentText;
        public string CommentText
        {
            private get { return _CommentText; }
            set { _CommentText = TruncateCommentContent(value); }
        }
        public string CommentUpvoteCount { get; set; }
        public string CommentDownvoteCount { get; set; }
        public string CommentUsername { get; set; }
        public string CommentProfilePicUrl { get; set; }
        public string CommentLink { get; set; }
        private string _CommentCreationTime;
        public DateTime CommentCreationTime
        {
            private get { return DateTime.MinValue; }
            set { _CommentCreationTime = FormatTimeAgo(value); }
        }
        private string _ParentCommentText;
        public string ParentCommentText
        {
            private get { return _ParentCommentText; }
            set { _ParentCommentText = TruncateCommentContent(value); }
        }
        public string ParentCommentUpvoteCount { get; set;  }
        public string ParentCommentDownvoteCount { get; set; }
        private string _ParentCommentCreationTime;
        public DateTime ParentCommentCreationTime 
        { 
            private get { return DateTime.MinValue; } 
            set { _ParentCommentCreationTime = FormatTimeAgo(value); } 
        }

        new protected void Substitute(ref string template)
        {
            base.Substitute(ref template);

            template = template.Replace("#CommentText#", CommentText);
            template = template.Replace("#CommentUpvoteCount#", CommentUpvoteCount);
            template = template.Replace("#CommentDownvoteCount#", CommentDownvoteCount);
            template = template.Replace("#CommentUsername#", CommentUsername);
            template = template.Replace("#CommentProfilePicUrl#", CommentProfilePicUrl);
            template = template.Replace("#CommentLink#", CommentLink);
            template = template.Replace("#CommentCreationTime#", _CommentCreationTime);
            template = template.Replace("#ParentCommentText#", ParentCommentText);
            template = template.Replace("#ParentCommentUpvoteCount#", ParentCommentUpvoteCount);
            template = template.Replace("#ParentCommentDownvoteCount#", ParentCommentDownvoteCount);
            template = template.Replace("#ParentCommentCreationTime#", _ParentCommentCreationTime);
        }

        protected string TruncateCommentContent(string value)
        {
            if(value.Length > 500)
            {
                return value.Substring(0, 500) + "...";
            }
            else
            {
                return value;
            }
        }
    }
}
