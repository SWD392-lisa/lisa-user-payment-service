using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

/// <summary>
/// Sổ cái bất biến. Không UPDATE/DELETE — chỉ INSERT. Dùng để audit, đối soát, và tái tính balance nếu cần.
/// </summary>
[Table("wallet_ledger")]
[Index("TransactionId", Name = "idx_wallet_ledger_txn")]
[Index("WalletId", "CreatedAt", Name = "idx_wallet_ledger_wallet", IsDescending = new[] { false, true })]
public partial class WalletLedger
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("wallet_id")]
    public Guid WalletId { get; set; }

    [Column("transaction_id")]
    public long TransactionId { get; set; }

    [Column("amount")]
    [Precision(15, 2)]
    public decimal Amount { get; set; }

    [Column("balance_after")]
    [Precision(15, 2)]
    public decimal BalanceAfter { get; set; }

    [Column("entry_type")]
    [StringLength(10)]
    public string EntryType { get; set; } = null!;

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("TransactionId")]
    [InverseProperty("WalletLedgers")]
    public virtual Transaction Transaction { get; set; } = null!;

    [ForeignKey("WalletId")]
    [InverseProperty("WalletLedgers")]
    public virtual Wallet Wallet { get; set; } = null!;
}
