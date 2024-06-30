using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public class UserCommentReplyNotifTemplatingData : CommentNotificationsTemplatingScaffold, ITemplate
    {
        public static string TemplateFile { private get; set; }
        public string GetTemplateFile() { return TemplateFile; }
        public string NotifDisableLink { get; set; }

        new public void Substitute(ref string template)
        {
            base.Substitute(ref template);

            template = template.Replace("#NotifDisableLink#", NotifDisableLink);
        }
    }
}
