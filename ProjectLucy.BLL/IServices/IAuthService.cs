using ProjectLucy.BLL.Base;
using ProjectLucy.Shared.Dtos.LoginDtos;
using ProjectLucy.Shared.Dtos.RefreshTokenDtos;
using ProjectLucy.Shared.Dtos.RegisterDtos;

namespace ProjectLucy.BLL.IServices
{
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate user with email and password.
        /// Returns access token + refresh token on success.
        /// </summary>
        Task<IServiceResult> LoginAsync(LoginRequest request);

        /// <summary>
        /// Register a new user account with default role.
        /// </summary>
        Task<IServiceResult> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Issue a new access token using a valid refresh token.
        /// </summary>
        Task<IServiceResult> RefreshTokenAsync(RefreshTokenRequest request);

        /// <summary>
        /// Revoke the given refresh token (logout).
        /// </summary>
        Task<IServiceResult> LogoutAsync(string refreshToken);
    }
}
