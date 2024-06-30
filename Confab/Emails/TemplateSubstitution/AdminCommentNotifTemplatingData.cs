using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public class AdminCommentNotifTemplatingData : AdminCommentNotifTemplatingScaffold, ITemplate
    {
        public static string TemplateFile { private get; set; }
        public string GetTemplateFile() { return TemplateFile; }

        public string ParentCommentUsername { get; set; }
        public string ParentCommentProfilePicUrl { get; set; }
        public string ParentCommentUserId { get; set; }
        public string ParentCommentUserEmail { get; set; }

        new public void Substitute(ref string template)
        {
            base.Substitute(ref template);

            template = template.Replace("#ParentCommentUsername#", ParentCommentUsername);
            template = template.Replace("#ParentCommentProfilePicUrl#", ParentCommentProfilePicUrl);
            template = template.Replace("#ParentCommentUserId#", ParentCommentUserId);
            template = template.Replace("#ParentCommentUserEmail#", ParentCommentUserEmail);
        }
    }
}
