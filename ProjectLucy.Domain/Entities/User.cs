using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

[Table("user")]
[Index("UserEmail", Name = "idx_user_email")]
[Index("UserPhoneNumber", Name = "idx_user_phone")]
[Index("UserEmail", Name = "user_user_email_key", IsUnique = true)]
[Index("UserPhoneNumber", Name = "user_user_phone_number_key", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("user_full_name")]
    [StringLength(255)]
    public string UserFullName { get; set; } = null!;

    [Column("user_birthday")]
    public DateOnly UserBirthday { get; set; }

    [Column("user_hash_password")]
    [StringLength(255)]
    public string UserHashPassword { get; set; } = null!;

    [Column("user_email")]
    [StringLength(255)]
    public string UserEmail { get; set; } = null!;

    [Column("user_phone_number")]
    [StringLength(30)]
    public string? UserPhoneNumber { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("suspended_at")]
    public DateTime? SuspendedAt { get; set; }

    [Column("suspension_reason")]
    public string? SuspensionReason { get; set; }

    [InverseProperty("Receiver")]
    public virtual ICollection<GiftTransaction> GiftTransactionReceivers { get; set; } = new List<GiftTransaction>();

    [InverseProperty("Sender")]
    public virtual ICollection<GiftTransaction> GiftTransactionSenders { get; set; } = new List<GiftTransaction>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("User")]
    public virtual ICollection<RoleUpgradeOrder> RoleUpgradeOrders { get; set; } = new List<RoleUpgradeOrder>();

    [InverseProperty("User")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    [InverseProperty("User")]
    public virtual Wallet? Wallet { get; set; }
}
