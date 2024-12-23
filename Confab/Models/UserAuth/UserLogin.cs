namespace Confab.Models.UserAuth
{
    public class UserLogin
    {
        private string _Email;
        public string Email { get { return _Email; } set { _Email = value.ToLower(); } }
        public string LoginCode { get; set; }
        public string Location { get; set; }
        public bool MergeAnonAccount { get; set; }
    }
}
