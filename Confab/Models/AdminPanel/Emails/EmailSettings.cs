namespace Confab.Models.AdminPanel.Emails
{
    public class EmailSettings
    {
        public string? Location { get; set; }
        public bool? AdminNotifGlobal { get; set; }
        public bool? AdminNotifEditGlobal { get; set; }
        public bool? AdminNotifLocal { get; set; }
        public bool? AdminNotifEditLocal { get; set; }
        public bool? UserNotifLocal { get; set; }
        public bool? UserNotifGlobal { get; set; }


    }
}
