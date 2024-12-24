namespace Confab.Models.UserAuth
{
    public class LoginResponse
    {
        public LoginOutcome Outcome { get; set; }
        public string ErrorMessage { get; set; }
        public string Token { get; set; }
        public string CaptchaSitekey { get; set; }
    }

    public enum LoginOutcome : short
    {
        Success = 0,
        EmailInvalidFailure = 1,
        EmailUserBannedFailure = 2,
        EmailCodeRequestRateLimitFailure = 3,
        EmailGenericFailure = 4,
        VerificationCodeInvalidFailure = 5,
        VerificationCodeExpiredFailure = 6,
        VerificationCodeGenericFailure = 7,
        VerificationEmailsRateLimit = 8,
        AuthenticationDisabled = 9,
        MaxNewSignupsLimitFailure = 10,
        CaptchaRequired = 11,
    }
}
