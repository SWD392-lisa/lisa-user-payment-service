using Microsoft.Extensions.Options;
using ProjectLucy.BLL.Base;
using ProjectLucy.BLL.IServices;
using ProjectLucy.BLL.Settings;
using ProjectLucy.DAL.Base;
using ProjectLucy.DAL.Entities;
using ProjectLucy.DAL.UnitOfWork;
using ProjectLucy.Shared.Dtos.LoginDtos;
using ProjectLucy.Shared.Dtos.LoginDtos.Childs;
using ProjectLucy.Shared.Dtos.RefreshTokenDtos;
using ProjectLucy.Shared.Dtos.RegisterDtos;

namespace ProjectLucy.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IGenericRepositories<User> _userRepo;
        private readonly IGenericRepositories<Role> _roleRepo;
        private readonly IGenericRepositories<RefreshToken> _refreshTokenRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly JwtSettings _jwtSettings;

        private const string DefaultRoleCode = "USER";

        public AuthService(
            IGenericRepositories<User> userRepo,
            IGenericRepositories<Role> roleRepo,
            IGenericRepositories<RefreshToken> refreshTokenRepo,
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

        // ─────────────────────────────────────────────────────────────
        // LOGIN
        // ─────────────────────────────────────────────────────────────
        public async Task<IServiceResult> LoginAsync(LoginRequest request)
        {
            // 1. Find user by email
            var users = await _userRepo.GetAllAsync(
                predicate: u => u.UserEmail == request.Email.ToLower().Trim(),
                asNoTracking: true
            );

            var foundUser = users.FirstOrDefault();

            if (foundUser == null)
                return new ServiceResult(401, "Invalid email or password");

            // 2. Verify password with BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Password, foundUser.UserHashPassword))
                return new ServiceResult(401, "Invalid email or password");

            // 3. Load role
            var roles = await _roleRepo.GetAllAsync(
                predicate: r => r.RoleId == foundUser.RoleId,
                asNoTracking: true
            );
            foundUser.Role = roles.FirstOrDefault()!;

            // 4. Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(foundUser);
            var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

            // 5. Persist refresh token
            var refreshTokenEntity = new RefreshToken
            {
                TokenId = Guid.NewGuid(),
                Token = refreshTokenValue,
                UserId = foundUser.UserId,
                ExpiredAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepo.CreateAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                User = new UserInfoDto
                {
                    UserId = foundUser.UserId,
                    FullName = foundUser.UserFullName,
                    Email = foundUser.UserEmail,
                    RoleCode = foundUser.Role?.RoleCode ?? string.Empty,
                    RoleName = foundUser.Role?.RoleName ?? string.Empty
                }
            };

            return new ServiceResult(200, "Login successful", response);
        }

        // ─────────────────────────────────────────────────────────────
        // REGISTER
        // ─────────────────────────────────────────────────────────────
        public async Task<IServiceResult> RegisterAsync(RegisterRequest request)
        {
            // 1. Check duplicate email
            var emailExists = await _userRepo.AnyAsync(
                u => u.UserEmail == request.Email.ToLower().Trim()
            );

            if (emailExists)
                return new ServiceResult(409, "Email is already registered");

            // 2. Check duplicate phone (if provided)
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var phoneExists = await _userRepo.AnyAsync(
                    u => u.UserPhoneNumber == request.PhoneNumber.Trim()
                );

                if (phoneExists)
                    return new ServiceResult(409, "Phone number is already registered");
            }

            // 3. Get default role
            var roles = await _roleRepo.GetAllAsync(
                predicate: r => r.RoleCode == DefaultRoleCode,
                asNoTracking: true
            );

            var defaultRole = roles.FirstOrDefault();
            if (defaultRole == null)
                return new ServiceResult(500, $"Default role '{DefaultRoleCode}' not found. Please seed roles first.");

            // 4. Hash password with BCrypt (work factor 12)
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

            // 5. Create user entity
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                UserFullName = request.FullName.Trim(),
                UserEmail = request.Email.ToLower().Trim(),
                UserHashPassword = hashedPassword,
                UserBirthday = request.Birthday,
                UserPhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                    ? null
                    : request.PhoneNumber.Trim(),
                RoleId = defaultRole.RoleId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepo.CreateAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            var response = new RegisterResponse
            {
                UserId = newUser.UserId,
                FullName = newUser.UserFullName,
                Email = newUser.UserEmail,
                Role = defaultRole.RoleName
            };

            return new ServiceResult(201, "Registration successful", response);
        }

        // ─────────────────────────────────────────────────────────────
        // REFRESH TOKEN
        // ─────────────────────────────────────────────────────────────
        public async Task<IServiceResult> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // 1. Find the refresh token in DB
            var tokenEntities = await _refreshTokenRepo.GetAllAsync(
                predicate: rt => rt.Token == request.RefreshToken,
                asNoTracking: false
            );

            var storedToken = tokenEntities.FirstOrDefault();

            if (storedToken == null)
                return new ServiceResult(401, "Invalid refresh token");

            if (storedToken.IsRevoked == true)
                return new ServiceResult(401, "Refresh token has been revoked");

            if (storedToken.ExpiredAt < DateTime.UtcNow)
                return new ServiceResult(401, "Refresh token has expired");

            // 2. Load user with role
            var users = await _userRepo.GetAllAsync(
                predicate: u => u.UserId == storedToken.UserId,
                asNoTracking: true
            );

            var user = users.FirstOrDefault();
            if (user == null)
                return new ServiceResult(401, "User not found");

            var roles = await _roleRepo.GetAllAsync(
                predicate: r => r.RoleId == user.RoleId,
                asNoTracking: true
            );
            user.Role = roles.FirstOrDefault()!;

            // 3. Revoke old refresh token
            storedToken.IsRevoked = true;
            await _refreshTokenRepo.UpdateAsync(storedToken);

            // 4. Issue new tokens
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                TokenId = Guid.NewGuid(),
                Token = newRefreshTokenValue,
                UserId = user.UserId,
                ExpiredAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepo.CreateAsync(newRefreshToken);
            await _unitOfWork.SaveChangesAsync();

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

            return new ServiceResult(200, "Token refreshed successfully", response);
        }

        // ─────────────────────────────────────────────────────────────
        // LOGOUT
        // ─────────────────────────────────────────────────────────────
        public async Task<IServiceResult> LogoutAsync(string refreshToken)
        {
            var tokenEntities = await _refreshTokenRepo.GetAllAsync(
                predicate: rt => rt.Token == refreshToken,
                asNoTracking: false
            );

            var storedToken = tokenEntities.FirstOrDefault();

            if (storedToken == null)
                return new ServiceResult(404, "Refresh token not found");

            if (storedToken.IsRevoked == true)
                return new ServiceResult(200, "Already logged out");

            storedToken.IsRevoked = true;
            await _refreshTokenRepo.UpdateAsync(storedToken);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(200, "Logged out successfully");
        }
    }
}
