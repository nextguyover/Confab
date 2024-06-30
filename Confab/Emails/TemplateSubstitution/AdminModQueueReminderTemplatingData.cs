using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public class AdminModQueueReminderTemplatingData : AllEmailsTemplatingScaffold, ITemplate
    {
        public static string TemplateFile { private get; set; }
        public string GetTemplateFile() { return TemplateFile; }

        private string _ModQueueInactivityTime;
        public DateTime ModQueueInactivityTime
        {
            private get { return DateTime.MinValue; }
            set { _ModQueueInactivityTime = FormatTimeAgo(value); }
        }
        private string _ModQueueReminderTime;
        public DateTime ModQueueReminderTime
        {
            private get { return DateTime.MinValue; }
            set { _ModQueueReminderTime = FormatTimeAgo(value); }
        }
        private string _ModQueueOldestItemAge;
        public DateTime ModQueueOldestItemAge
        {
            private get { return DateTime.MinValue; }
            set { _ModQueueOldestItemAge = FormatTimeAgo(value); }
        }
        public int ModQueueCount { get; set; }

        new public void Substitute(ref string template)
        {
            base.Substitute(ref template);

            template = template.Replace("#ModQueueInactivityTime#", _ModQueueInactivityTime);
            template = template.Replace("#ModQueueReminderTime#", _ModQueueReminderTime);
            template = template.Replace("#ModQueueOldestItemAge#", _ModQueueOldestItemAge);
            template = template.Replace("#ModQueueCount#", ModQueueCount.ToString());
        }
    }
}
