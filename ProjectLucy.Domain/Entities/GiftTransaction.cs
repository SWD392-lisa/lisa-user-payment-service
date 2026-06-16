using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

/// <summary>
/// Chi tiết tặng quà. transaction_id trỏ đến bản ghi DEBIT trong transactions. Luồng: sender ví DEBIT → wallet_ledger → gift_transaction. Receiver nhận CREDIT riêng qua transaction mới.
/// </summary>
[Table("gift_transaction")]
[Index("TransactionId", Name = "gift_transaction_transaction_id_key", IsUnique = true)]
[Index("ReceiverId", "CreatedAt", Name = "idx_gift_txn_receiver", IsDescending = new[] { false, true })]
[Index("SenderId", "CreatedAt", Name = "idx_gift_txn_sender", IsDescending = new[] { false, true })]
[Index("RoomSessionId", Name = "idx_gift_txn_session")]
public partial class GiftTransaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("transaction_id")]
    public long TransactionId { get; set; }

    [Column("sender_id")]
    public Guid SenderId { get; set; }

    [Column("receiver_id")]
    public Guid ReceiverId { get; set; }

    [Column("gift_id")]
    public Guid GiftId { get; set; }

    [Column("room_session_id")]
    public Guid? RoomSessionId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("total_value")]
    [Precision(10, 2)]
    public decimal TotalValue { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("GiftId")]
    [InverseProperty("GiftTransactions")]
    public virtual GiftCatalog Gift { get; set; } = null!;

    [ForeignKey("ReceiverId")]
    [InverseProperty("GiftTransactionReceivers")]
    public virtual User Receiver { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("GiftTransactionSenders")]
    public virtual User Sender { get; set; } = null!;

    [ForeignKey("TransactionId")]
    [InverseProperty("GiftTransaction")]
    public virtual Transaction Transaction { get; set; } = null!;
}
