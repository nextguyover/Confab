namespace Confab.Emails
{
    public class SmptMailbox
    {
        public string Address;
        public string Username;
        public string Password;

        public SmptMailbox(string address, string username, string password)
        {
            Address = address;
            Username = username;
            Password = password;
        }
    }
}
