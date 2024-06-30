using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Confab.Data.DatabaseModels
{
    public class UserSchema
    {
        public int Id { get; set; }
        public string PublicId { get; set; }
        public UserRole Role { get; set; }
        public bool IsBanned { get; set; }
        public string Username { get; set; }

        public string Email { get; set; }       //TODO make this unique at db level

        public string VerificationCode { get; set; }
        public DateTime VerificationExpiry { get; set; }
        public int VerificationCodeAttempts { get; set; }

        public int VerificationCodeEmailCount { get; set; }     //Number of verification emails sent (resets on sucessful login, or if certain duration has elapsed since first verification email sent
        public DateTime VerificationCodeFirstEmail { get; set; }    //Timestamp of first verification code email sent (resets on sucessful login)


        [InverseProperty("Author")]
        public List<CommentSchema> Comments { get; set; } = new List<CommentSchema>();


        [InverseProperty("UpvotedUsers")]
        public List<CommentSchema> UpvotedComments { get; set; } = new List<CommentSchema>();
        [InverseProperty("DownvotedUsers")]
        public List<CommentSchema> DownvotedComments { get; set; } = new List<CommentSchema>();

        public DateTime AccountCreation { get; set; }
        public DateTime LastActive { get; set; }
        public DateTime LastUsernameChange { get; set; }

        public bool ReplyNotificationsEnabled { get; set; } = true;
    }

    public enum UserRole : short
    {
        Standard = 0,
        Admin = 1,
    }
}
