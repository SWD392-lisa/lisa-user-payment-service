using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

[Table("gift_event_outbox")]
[Index(nameof(GiftTransactionId), IsUnique = true, Name = "uq_gift_event_outbox_transaction")]
[Index(nameof(Status), nameof(NextAttemptAt), Name = "idx_gift_event_outbox_pending")]
public class GiftEventOutbox
{
    [Key, Column("id")]
    public Guid Id { get; set; }
    [Column("gift_transaction_id")]
    public Guid GiftTransactionId { get; set; }
    [Column("payload", TypeName = "jsonb")]
    public string Payload { get; set; } = null!;
    [Column("status"), StringLength(20)]
    public string Status { get; set; } = "PENDING";
    [Column("attempts")]
    public int Attempts { get; set; }
    [Column("next_attempt_at")]
    public DateTime NextAttemptAt { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("sent_at")]
    public DateTime? SentAt { get; set; }
    [Column("last_error")]
    public string? LastError { get; set; }
}
