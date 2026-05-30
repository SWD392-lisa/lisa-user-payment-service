using ProjectLucy.Domain.Entities;
using System.Security.Claims;

namespace ProjectLucy.Application.Interfaces;

/// <summary>
/// JWT token operations. Defined in Application so handlers can depend on it
/// without referencing the Infrastructure JWT implementation.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
