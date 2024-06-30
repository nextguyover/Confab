using Confab.Data.DatabaseModels;

namespace Confab.Models
{
    public class UserCreate
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
    }
}
