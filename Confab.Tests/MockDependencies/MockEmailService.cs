using Confab.Emails.TemplateSubstitution.Interfaces;
using Confab.Emails;
using Confab.Services;
using MimeKit;

namespace Confab.Tests.MockDependencies
{
    public class MockEmailService : EmailService
    {
        public static List<ITemplate> SentMessages = new List<ITemplate>();

        override protected Task<bool> _SendEmail(ITemplate emailTemplate, MimeMessage emailMessage, SmptMailbox fromMailbox)
        {
            SentMessages.Add(emailTemplate);
            return Task.FromResult(true);
        }
    }
}
