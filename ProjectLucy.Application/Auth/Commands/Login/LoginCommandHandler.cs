using MediatR;
using ProjectLucy.Application.DTOs.LoginDtos;
using ProjectLucy.Application.DTOs.LoginDtos.Childs;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Application.Settings;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace ProjectLucy.Application.Auth.Commands.Login;

/// <summary>
/// Handles the LoginCommand.
/// Logic is identical to the old AuthService.LoginAsync — only dependencies changed:
///   Before: NeondbContext (direct) + IGenericRepositories
///   After:  IUserRepository + IRoleRepository (clean interfaces, no EF leakage)
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public LoginCommandHandler(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        // 1. Find user by email
        var user = await _userRepo.GetByEmailAsync(cmd.Request.Email.ToLower().Trim(), ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(cmd.Request.Password, user.UserHashPassword))
            throw new UnauthorizedException("Invalid email or password");

        // 2. Load role (user.Role may be null if repo didn't eager-load it)
        if (user.Role is null)
            user.Role = (await _roleRepo.GetByIdAsync(user.RoleId, ct))!;

        if (user.Role is null)
            throw new ServerException("Role associated with the user was not found");

        // 3. Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        // 4. Persist refresh token
        var refreshTokenEntity = new ProjectLucy.Domain.Entities.RefreshToken
        {
            TokenId = Guid.NewGuid(),
            Token = refreshTokenValue,
            UserId = user.UserId,
            ExpiredAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepo.AddAsync(refreshTokenEntity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
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

        return Result<LoginResponse>.Success(response, "Login successful");
    }
}
