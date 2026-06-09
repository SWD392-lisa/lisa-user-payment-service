using MediatR;
using Microsoft.Extensions.Options;
using ProjectLucy.Application.DTOs;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Application.Settings;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Handles token rotation: validates the old refresh token, revokes it,
/// issues a new access token + refresh token pair.
/// Logic mirrors the old AuthService.RefreshTokenAsync exactly.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepo,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _refreshTokenRepo = refreshTokenRepo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        // 1. Find the stored refresh token
        var storedToken = await _refreshTokenRepo.GetByTokenAsync(cmd.RefreshToken, ct);

        if (storedToken is null)
            throw new UnauthorizedException("Invalid refresh token");

        if (storedToken.IsRevoked == true)
            throw new UnauthorizedException("Refresh token has been revoked");

        if (storedToken.ExpiredAt < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired");

        // 2. Load user with role
        var user = await _userRepo.GetByIdAsync(storedToken.UserId, ct);
        if (user is null)
            throw new UnauthorizedException("User not found");

        if (user.Role is null)
            user.Role = (await _roleRepo.GetByIdAsync(user.RoleId, ct))!;

        if (user.Role is null)
            throw new ServerException("Role associated with the user was not found");

        // 3. Revoke old token
        storedToken.IsRevoked = true;
        await _refreshTokenRepo.UpdateAsync(storedToken, ct);

        // 4. Issue new tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var newRefreshToken = new ProjectLucy.Domain.Entities.RefreshToken
        {
            TokenId = Guid.NewGuid(),
            Token = newRefreshTokenValue,
            UserId = user.UserId,
            ExpiredAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepo.AddAsync(newRefreshToken, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var response = new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenValue,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            User = new UserInfoDto
            {
                UserId = user.UserId,
                FullName = user.UserFullName,
                Email = user.UserEmail,
                RoleCode = user.Role?.RoleCode ?? string.Empty,
                RoleName = user.Role?.RoleName ?? string.Empty
            }
        };

        return Result<LoginResponse>.Success(response, "Token refreshed successfully");
    }
}
