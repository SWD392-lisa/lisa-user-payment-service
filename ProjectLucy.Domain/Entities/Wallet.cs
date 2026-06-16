using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

/// <summary>
/// Ví điện tử 1-1 với user. Balance luôn &gt;= 0, mọi thay đổi phải qua wallet_ledger.
/// </summary>
[Table("wallet")]
[Index("UserId", Name = "wallet_user_id_key", IsUnique = true)]
public partial class Wallet
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("balance")]
    [Precision(15, 2)]
    public decimal Balance { get; set; }

    [Column("currency")]
    [StringLength(10)]
    public string Currency { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Wallet")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Wallet")]
    public virtual ICollection<WalletLedger> WalletLedgers { get; set; } = new List<WalletLedger>();
}
