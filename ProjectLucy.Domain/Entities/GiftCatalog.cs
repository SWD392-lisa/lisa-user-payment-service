using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

/// <summary>
/// Danh mục quà ảo có thể tặng trong phòng học. is_active = FALSE để ẩn khỏi UI mà không xóa lịch sử.
/// </summary>
[Table("gift_catalog")]
[Index("IsActive", Name = "idx_gift_catalog_active")]
public partial class GiftCatalog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("icon_url")]
    public string? IconUrl { get; set; }

    [Column("price")]
    [Precision(10, 2)]
    public decimal Price { get; set; }

    [Column("currency")]
    [StringLength(10)]
    public string Currency { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Gift")]
    public virtual ICollection<GiftTransaction> GiftTransactions { get; set; } = new List<GiftTransaction>();
}
