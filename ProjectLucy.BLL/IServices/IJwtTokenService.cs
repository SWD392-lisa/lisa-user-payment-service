using ProjectLucy.DAL.Entities;
using System.Security.Claims;

namespace ProjectLucy.BLL.IServices
{
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generate a signed JWT access token for the given user.
        /// </summary>
        string GenerateAccessToken(User user);

        /// <summary>
        /// Generate a cryptographically random refresh token string.
        /// </summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Validate an access token and return its claims principal.
        /// Returns null if the token is invalid or expired.
        /// </summary>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
