namespace ProjectLucy.Domain.Entities;

/// <summary>
/// Core user entity. No EF or framework dependencies here.
/// EF configuration lives in Infrastructure/Persistence.
/// </summary>
public class User
{
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = null!;
    public DateOnly UserBirthday { get; set; }
    public string UserHashPassword { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string? UserPhoneNumber { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int RoleId { get; set; }

    // Navigation — kept for EF convenience; Infrastructure owns the mapping
    public virtual Role Role { get; set; } = null!;
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
