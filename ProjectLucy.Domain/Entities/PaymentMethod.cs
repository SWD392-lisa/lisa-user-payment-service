using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

/// <summary>
/// Lưu chi tiết từ cổng thanh toán (VNPAY/Momo/ZaloPay...). metadata giữ nguyên payload gốc để debug &amp; đối soát.
/// </summary>
[Table("payment_method")]
[Index("Provider", Name = "idx_payment_method_provider")]
[Index("ProviderTxnId", Name = "idx_payment_method_provider_txn")]
[Index("TransactionId", Name = "payment_method_transaction_id_key", IsUnique = true)]
public partial class PaymentMethod
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("transaction_id")]
    public long TransactionId { get; set; }

    [Column("provider")]
    [StringLength(50)]
    public string Provider { get; set; } = null!;

    [Column("provider_txn_id")]
    [StringLength(255)]
    public string? ProviderTxnId { get; set; }

    [Column("raw_status")]
    [StringLength(50)]
    public string? RawStatus { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("TransactionId")]
    [InverseProperty("PaymentMethod")]
    public virtual Transaction Transaction { get; set; } = null!;
}
