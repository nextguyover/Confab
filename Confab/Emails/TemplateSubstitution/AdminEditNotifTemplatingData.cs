using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public class AdminEditNotifTemplatingData : AdminCommentNotifTemplatingScaffold, ITemplate
    {
        public static string TemplateFile { private get; set; }
        public string GetTemplateFile() { return TemplateFile; }

        private string _EditPreviousContent;
        public string EditPreviousContent
        {
            private get { return _EditPreviousContent; }
            set { _EditPreviousContent = TruncateCommentContent(value); }
        }
        new public readonly string ParentCommentText;
        new public readonly string ParentCommentUpvoteCount;
        new public readonly string ParentCommentDownvoteCount;
        new public readonly string ParentCommentCreationTime;

        new public void Substitute(ref string template)
        {
            base.Substitute(ref template);

            template = template.Replace("#EditPreviousContent#", EditPreviousContent);
        }
    }
}
