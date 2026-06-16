using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

/// <summary>
/// Đơn hàng nâng cấp tài khoản (Pro/Super). activated_at được set khi transaction → completed. expires_at = activated_at + role_price.duration (NULL = không hết hạn).
/// </summary>
[Table("role_upgrade_order")]
[Index("UserId", "CreatedAt", Name = "idx_upgrade_user", IsDescending = new[] { false, true })]
[Index("TransactionId", Name = "role_upgrade_order_transaction_id_key", IsUnique = true)]
public partial class RoleUpgradeOrder
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("transaction_id")]
    public long TransactionId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("from_role_id")]
    public int FromRoleId { get; set; }

    [Column("to_role_id")]
    public int ToRoleId { get; set; }

    [Column("role_price_id")]
    public int RolePriceId { get; set; }

    [Column("activated_at")]
    public DateTime? ActivatedAt { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("FromRoleId")]
    [InverseProperty("RoleUpgradeOrderFromRoles")]
    public virtual Role FromRole { get; set; } = null!;

    [ForeignKey("RolePriceId")]
    [InverseProperty("RoleUpgradeOrders")]
    public virtual RolePrice RolePrice { get; set; } = null!;

    [ForeignKey("ToRoleId")]
    [InverseProperty("RoleUpgradeOrderToRoles")]
    public virtual Role ToRole { get; set; } = null!;

    [ForeignKey("TransactionId")]
    [InverseProperty("RoleUpgradeOrder")]
    public virtual Transaction Transaction { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("RoleUpgradeOrders")]
    public virtual User User { get; set; } = null!;
}
