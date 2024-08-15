using Confab.Data.DatabaseModels;

namespace Confab.Models
{
    public class User
    {
        public string Username { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public bool IsAnon { get; set; }
    }
}
