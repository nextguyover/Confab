using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public class AdminCommentNotifTopLvlTemplatingData : AdminCommentNotifTemplatingScaffold, ITemplate
    {
        public static string TemplateFile { private get; set; }
        new public readonly string ParentCommentText;
        new public readonly string ParentCommentUpvoteCount;
        new public readonly string ParentCommentDownvoteCount;
        new public readonly string ParentCommentCreationTime;
        public string GetTemplateFile() { return TemplateFile; }
    }
}
