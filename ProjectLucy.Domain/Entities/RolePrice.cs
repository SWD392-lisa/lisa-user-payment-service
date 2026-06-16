using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

[Table("role_price")]
[Index("IsActive", Name = "idx_role_price_active")]
[Index("RoleId", Name = "idx_role_price_role_id")]
public partial class RolePrice
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("price")]
    [Precision(12, 2)]
    public decimal Price { get; set; }

    [Column("currency")]
    [StringLength(10)]
    public string? Currency { get; set; }

    [Column("duration")]
    public TimeSpan? Duration { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("RoleId")]
    [InverseProperty("RolePrices")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("RolePrice")]
    public virtual ICollection<RoleUpgradeOrder> RoleUpgradeOrders { get; set; } = new List<RoleUpgradeOrder>();
}
