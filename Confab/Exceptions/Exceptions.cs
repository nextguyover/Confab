namespace Confab.Exceptions
{
    public class InvalidCommentIdException : Exception { }
    public class InvalidEmailException : Exception { }
    public class RateLimitException : Exception { }
    public class UserLoginFailedException : Exception { }
    public class UserLoginVerificationCodeExpiredException : Exception { }
    public class UserNotFoundException : Exception { }
    public class MissingAuthorizationException : Exception { }
    public class InvalidCommentVoteException : Exception { }
    public class InvalidUsernameException : Exception { }
    public class InvalidCommentException : Exception { }
    public class CommentNotEditableException : Exception { }
    public class InvalidAuthorizationException : Exception { }
    public class UserBannedException : Exception { }
    public class UninitialisedLocationException : Exception { }
    public class EmailSendErrorException : Exception { }
    public class VerificationEmailsRateLimitException : Exception { }
    public class MaxNewSignupsLimitException : Exception { }
    public class InvalidConfigException : Exception  { public InvalidConfigException(string message) : base(message){} }
    public class CommentAwaitingModerationException : Exception { }
    public class UserReachedModQueueMaxCountException : Exception { }
    public class VotingNotEnabledException : Exception { }
    public class CommentingNotEnabledException : Exception { }
    public class AccountCreationDisabledException : Exception { }
    public class UserLoginDisabledException : Exception { }
    public class InvalidLocationException : Exception { }
    public class AutoModerationFailedException : Exception { public string Feedback; public AutoModerationFailedException(string feedback) { Feedback = feedback; } }
    public class UsernameUnavailableException : Exception { }
    public class CustomUsernameNotAllowedException : Exception { }
    public class UserCommentRateLimitException : Exception { }
    public class EditHistoryDisabledException : Exception { }
}
