using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public abstract class AdminCommentNotifTemplatingScaffold : CommentNotificationsTemplatingScaffold
    {
        public string CommentUserId { get; set; }
        public string CommentUserEmail { get; set; }
        public string CommentLocationInDb { get; set; }

        new public void Substitute(ref string template)
        {
            base.Substitute(ref template);

            template = template.Replace("#CommentUserId#", CommentUserId);
            template = template.Replace("#CommentUserEmail#", CommentUserEmail);
            template = template.Replace("#CommentLocationInDb#", CommentLocationInDb);
        }
    }
}
