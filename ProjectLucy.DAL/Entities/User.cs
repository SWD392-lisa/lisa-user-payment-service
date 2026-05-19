using System;
using System.Collections.Generic;

namespace ProjectLucy.DAL.Entities;

public partial class User
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

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual Role Role { get; set; } = null!;
}
