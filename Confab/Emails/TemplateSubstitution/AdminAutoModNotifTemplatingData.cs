using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public class AdminAutoModNotifTemplatingData : AdminCommentNotifTemplatingScaffold, ITemplate
    {
        public static string TemplateFile { private get; set; }
        public string GetTemplateFile() { return TemplateFile; }

        public string AutoModRuleRegex { get; set; }
        public string AutoModRuleAction { get; set; }
        new public readonly string ParentCommentText;
        new public readonly string ParentCommentUpvoteCount;
        new public readonly string ParentCommentDownvoteCount;
        new public readonly string ParentCommentCreationTime;

        new public void Substitute(ref string template)
        {
            base.Substitute(ref template);

            template = template.Replace("#AutoModRuleRegex#", AutoModRuleRegex);
            template = template.Replace("#AutoModRuleAction#", AutoModRuleAction);
        }
    }
}
