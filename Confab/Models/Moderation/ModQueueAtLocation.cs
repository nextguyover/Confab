namespace Confab.Models.Moderation
{
    public class ModQueueAtLocation
    {
        public string location {  get; set; }
        public List<ModQueueComment> comments { get; set; } = new List<ModQueueComment>();
    }
}
