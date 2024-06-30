namespace Confab.Models.AdminPanel.Statistics
{
    public class Statistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers_24h { get; set; }
        public int ActiveUsers_7d { get; set; }
        public int ActiveUsers_30d { get; set; }
        public int ActiveUsers_1y { get; set; }

        public int TotalComments { get; set; }
        public int NewComments_24h { get; set; }
        public int NewComments_7d { get; set; }
        public int NewComments_30d { get; set; }
        public int NewComments_1y { get; set; }
    }
}
