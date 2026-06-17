using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ProjectLucy.Application.Auth.Commands.Login;
using ProjectLucy.Application.Auth.Commands.Logout;
using ProjectLucy.Application.Auth.Commands.RefreshToken;
using ProjectLucy.Application.Auth.Commands.Register;
using ProjectLucy.Application.DTOs.LoginDtos;
using ProjectLucy.Application.DTOs.LogoutDtos;
using ProjectLucy.Application.DTOs.RefreshTokenDtos;
using ProjectLucy.Application.DTOs.RegisterDtos;

namespace ProjectLucy.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        private const string RefreshTokenCookieName = "refreshToken";

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        // ─────────────────────────────────────────────────────────────
        // POST /api/auth/login
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Authenticate with email and password.
        /// Returns access token in body and sets refresh token in HttpOnly cookie.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _sender.Send(new LoginCommand(request));

            if (result.Status != 200)
                return StatusCode(result.Status, result);

            if (result.Data is LoginResponse loginResponse)
                SetRefreshTokenCookie(loginResponse.RefreshToken);

            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────
        // POST /api/auth/register
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Register a new user account.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _sender.Send(new RegisterCommand(request));

            return StatusCode(result.Status, result);
        }

        // ─────────────────────────────────────────────────────────────
        // POST /api/auth/refresh-token
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Issue a new access token using the refresh token.
        /// Reads refresh token from HttpOnly cookie first, falls back to request body.
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
        {
            // Prefer cookie over body (cookie is more secure)
            var refreshTokenValue = Request.Cookies[RefreshTokenCookieName]
                ?? request?.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshTokenValue))
                return BadRequest(new { Status = 400, Message = "Refresh token is required" });

            var result = await _sender.Send(
                new RefreshTokenCommand(new RefreshTokenRequest { RefreshToken = refreshTokenValue }));

            if (result.Status != 200)
            {
                Response.Cookies.Delete(RefreshTokenCookieName);
                return StatusCode(result.Status, result);
            }

            // Rotate cookie with new refresh token
            if (result.Data is LoginResponse loginResponse)
                SetRefreshTokenCookie(loginResponse.RefreshToken);

            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────
        // POST /api/auth/logout
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Revoke the refresh token and clear the cookie.
        /// Reads refresh token from HttpOnly cookie first, falls back to request body.
        /// </summary>
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
        {
            var refreshTokenValue = Request.Cookies[RefreshTokenCookieName]
                ?? request?.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                Response.Cookies.Delete(RefreshTokenCookieName);
                return Ok(new { Status = 200, Message = "Logged out successfully" });
            }

            var result = await _sender.Send(new LogoutCommand(refreshTokenValue));

            // Always clear the cookie regardless of result
            Response.Cookies.Delete(RefreshTokenCookieName);

            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Set the refresh token as an HttpOnly, Secure, SameSite=Strict cookie.
        /// The FE should also store it in localStorage for cross-tab access.
        /// </summary>
        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Allow non-HTTPS for development
                SameSite = SameSiteMode.Lax, // Relax SameSite for development
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/"
            };

            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
        }
    }
}
