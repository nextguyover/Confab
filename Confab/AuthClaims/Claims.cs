using Confab.Exceptions;
using Confab.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Confab.AuthClaims
{
    public class Claims
    {
        public static string Email = "Email";
        //public static string Role = "Role";
        //public static string PublicId = "PublicId";
        //public static string IsBanned = "IsBanned";

        public static string GetClaim(HttpContext context, DateTime ValidityStart, string claimType)
        {
            var handler = new JwtSecurityTokenHandler();
            string authHeader = context.Request.Headers["Authorization"];

            if (authHeader.IsNullOrEmpty()) throw new MissingAuthorizationException();

            JwtSecurityToken jwtToken = null;
            try
            {
                authHeader = authHeader.Replace("Bearer ", "");

                handler.ValidateToken(authHeader, UserService.JwtValidationParams, out SecurityToken validatedToken);

                jwtToken = (JwtSecurityToken)validatedToken;
            }
            catch
            {
                throw new MissingAuthorizationException();
            }

            DateTime tokenCreation = DateTimeOffset.FromUnixTimeSeconds(long.Parse(jwtToken.Claims.First(claim => claim.Type == "nbf").Value)).UtcDateTime;
            DateTime tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(long.Parse(jwtToken.Claims.First(claim => claim.Type == "exp").Value)).UtcDateTime;

            if (tokenCreation < ValidityStart.AddSeconds(-5)) throw new MissingAuthorizationException();
            if (tokenExpiry < DateTime.UtcNow) throw new MissingAuthorizationException();

            var claimVal = jwtToken.Claims.First(claim => claim.Type == claimType).Value;

            return claimVal;

        }
    }
}
