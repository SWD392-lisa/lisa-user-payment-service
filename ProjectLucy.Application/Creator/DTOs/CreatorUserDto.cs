namespace ProjectLucy.Application.Creator.DTOs;

public sealed class CreatorUserDto
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public DateOnly Birthday { get; init; }
    public string RoleCode { get; init; } = null!;
    public string RoleName { get; init; } = null!;
    public bool IsActive { get; init; }
    public DateTime? SuspendedAt { get; init; }
    public string? SuspensionReason { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
