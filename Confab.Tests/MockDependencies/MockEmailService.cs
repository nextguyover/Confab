using Confab.Emails.TemplateSubstitution.Interfaces;
using Confab.Emails;
using Confab.Services;
using MimeKit;

namespace Confab.Tests.MockDependencies
{
    public class MockEmailService : EmailService
    {
        public static Dictionary<string, List<ITemplate>> SentMessages = new Dictionary<string, List<ITemplate>>();

        override protected Task<bool> _SendEmail(ITemplate emailTemplate, MimeMessage emailMessage, SmptMailbox fromMailbox)
        {
            if (!SentMessages.ContainsKey(emailMessage.To.Mailboxes.First().Address)) {
                SentMessages[emailMessage.To.Mailboxes.First().Address] = new List<ITemplate>();
            }
            SentMessages[emailMessage.To.Mailboxes.First().Address].Add(emailTemplate);
            return Task.FromResult(true);
        }
    }
}
