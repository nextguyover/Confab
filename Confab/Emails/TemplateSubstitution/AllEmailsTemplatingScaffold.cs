using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public abstract class AllEmailsTemplatingScaffold
    {
        public static string ServiceName { get; set; }
        public static string SiteUrl { get; set; }
        public static string ConfabBackendApiUrl { get; set; }

        public string UserEmail { get; set; }
        public string Username { get; set; }
        public string UserProfilePicUrl { get; set; }

        public const string ConfabUrl = "confabcomments.com";

        protected void Substitute(ref string template)
        {
            template = template.Replace("#ServiceName#", ServiceName);
            template = template.Replace("#SiteUrl#", SiteUrl);
            template = template.Replace("#ConfabBackendApiUrl#", ConfabBackendApiUrl);
            template = template.Replace("#UserEmail#", UserEmail);
            template = template.Replace("#Username#", Username);
            template = template.Replace("#UserProfilePicUrl#", UserProfilePicUrl);
            template = template.Replace("#ConfabUrl#", ConfabUrl);
            template = template.Replace("#EmailTimestamp#", DateTime.UtcNow.ToString("HH:mm:ss dd/MM/yyyy") + " UTC");
        }

        protected static string FormatTimeAgo(DateTime dateTimeVal)
        {
            TimeSpan timeDifference = DateTime.UtcNow - dateTimeVal;

            int seconds = (int)Math.Floor(timeDifference.TotalSeconds);
            int minutes = (int)Math.Floor(timeDifference.TotalMinutes);
            int hours = (int)Math.Floor(timeDifference.TotalHours);
            int days = (int)Math.Floor(timeDifference.TotalDays);
            int years = (int)Math.Floor(days / 365.25); // accounting for leap years

            if (years >= 1)
            {
                return $"{years} year{(years > 1 ? "s" : "")}";
            }
            else if (days >= 1)
            {
                return $"{days} day{(days > 1 ? "s" : "")}";
            }
            else if (hours >= 1)
            {
                return $"{hours} hour{(hours > 1 ? "s" : "")}";
            }
            else if (minutes >= 1)
            {
                return $"{minutes} minute{(minutes > 1 ? "s" : "")}";
            }
            else
            {
                return $"{seconds} second{(seconds != 1 ? "s" : "")}";
            }
        }
    }
}
