using MediatR;
using ProjectLucy.Application.DTOs;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Auth.Commands.Register;

/// <summary>
/// Handles RegisterCommand.
/// Logic mirrors the old AuthService.RegisterAsync exactly.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUnitOfWork _unitOfWork;

    private const string DefaultRoleCode = "LUCY";

    public RegisterCommandHandler(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IUnitOfWork unitOfWork)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        // Note: duplicate email/phone are caught by RegisterCommandValidator
        // before reaching the handler. The unique indexes on user_email and
        // user_phone_number in the database still protect against race conditions.

        // 1. Get default role
        var defaultRole = await _roleRepo.GetByCodeAsync(DefaultRoleCode, ct);
        if (defaultRole is null)
            throw new ServerException($"Default role '{DefaultRoleCode}' not found. Please seed roles first.");

        // 4. Hash password (work factor 12 — same as before)
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(cmd.Password, workFactor: 12);

        // 5. Create user entity
        var newUser = new User
        {
            UserId = Guid.NewGuid(),
            UserFullName = cmd.FullName.Trim(),
            UserEmail = cmd.Email.ToLower().Trim(),
            UserHashPassword = hashedPassword,
            UserBirthday = cmd.Birthday,
            UserPhoneNumber = string.IsNullOrWhiteSpace(cmd.PhoneNumber)
                ? null
                : cmd.PhoneNumber.Trim(),
            RoleId = defaultRole.RoleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(newUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<RegisterResponse>.Created(
            new RegisterResponse { IsSuccess = true },
            "Registration successful");
    }
}
