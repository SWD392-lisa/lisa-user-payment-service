using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

[Table("transactions")]
[Index("CreatedAt", Name = "idx_transactions_created_at")]
[Index("ReferenceCode", Name = "idx_transactions_reference_code")]
[Index("Status", Name = "idx_transactions_status")]
[Index("TransactionTypeId", Name = "idx_transactions_type_id")]
[Index("UserId", Name = "idx_transactions_user_id")]
[Index("ReferenceCode", Name = "transactions_reference_code_key", IsUnique = true)]
public partial class Transaction
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("transaction_type_id")]
    public int TransactionTypeId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("amount")]
    [Precision(15, 2)]
    public decimal Amount { get; set; }

    [Column("currency")]
    [StringLength(10)]
    public string? Currency { get; set; }

    [Column("transaction_content")]
    public string? TransactionContent { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("reference_code")]
    [StringLength(100)]
    public string? ReferenceCode { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Transaction")]
    public virtual GiftTransaction? GiftTransaction { get; set; }

    [InverseProperty("Transaction")]
    public virtual PaymentMethod? PaymentMethod { get; set; }

    [InverseProperty("Transaction")]
    public virtual RoleUpgradeOrder? RoleUpgradeOrder { get; set; }

    [ForeignKey("TransactionTypeId")]
    [InverseProperty("Transactions")]
    public virtual TransactionType TransactionType { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Transactions")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Transaction")]
    public virtual ICollection<WalletLedger> WalletLedgers { get; set; } = new List<WalletLedger>();
}
