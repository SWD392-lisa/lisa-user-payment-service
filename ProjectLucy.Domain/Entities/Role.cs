using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

[Table("role")]
[Index("RoleCode", Name = "role_role_code_key", IsUnique = true)]
public partial class Role
{
    [Key]
    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("role_code")]
    [StringLength(50)]
    public string RoleCode { get; set; } = null!;

    [Column("role_name")]
    [StringLength(100)]
    public string RoleName { get; set; } = null!;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Role")]
    public virtual ICollection<RolePrice> RolePrices { get; set; } = new List<RolePrice>();

    [InverseProperty("FromRole")]
    public virtual ICollection<RoleUpgradeOrder> RoleUpgradeOrderFromRoles { get; set; } = new List<RoleUpgradeOrder>();

    [InverseProperty("ToRole")]
    public virtual ICollection<RoleUpgradeOrder> RoleUpgradeOrderToRoles { get; set; } = new List<RoleUpgradeOrder>();

    [InverseProperty("Role")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
