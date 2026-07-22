using ProjectLucy.Application.Common.Exceptions;
using ProjectLucy.Application.Creator.DTOs;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Creator;

public sealed class CreatorUserService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public CreatorUserService(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreatorUsersPageDto> SearchAsync(
        string? search,
        string? roleCode,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _users.SearchAsync(search, roleCode, isActive, (page - 1) * pageSize, pageSize, ct);
        return new CreatorUsersPageDto
        {
            Items = result.Items.Select(ToDto).ToList(),
            Total = result.Total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<CreatorUserDto> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found");
        return ToDto(user);
    }

    public async Task<CreatorUserDto> UpdateStatusAsync(
        Guid userId,
        Guid actorUserId,
        UpdateUserStatusRequest request,
        CancellationToken ct)
    {
        if (userId == actorUserId)
            throw new BadRequestException("You cannot suspend your own account");

        var user = await _users.GetByIdTrackedAsync(userId, ct)
            ?? throw new NotFoundException("User not found");

        if (!request.IsActive && string.IsNullOrWhiteSpace(request.Reason))
            throw new BadRequestException("A reason is required when suspending an account");

        user.IsActive = request.IsActive;
        user.SuspendedAt = request.IsActive ? null : DateTime.UtcNow;
        user.SuspensionReason = request.IsActive ? null : request.Reason!.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        if (!request.IsActive)
            await _users.RevokeTokensAsync(userId, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(user);
    }

    private static CreatorUserDto ToDto(User user) => new()
    {
        UserId = user.UserId,
        FullName = user.UserFullName,
        Email = user.UserEmail,
        PhoneNumber = user.UserPhoneNumber,
        Birthday = user.UserBirthday,
        RoleCode = user.Role?.RoleCode ?? string.Empty,
        RoleName = user.Role?.RoleName ?? string.Empty,
        IsActive = user.IsActive,
        SuspendedAt = user.SuspendedAt,
        SuspensionReason = user.SuspensionReason,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
    };
}
