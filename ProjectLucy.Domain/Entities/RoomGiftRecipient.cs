using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

[Table("room_gift_recipient")]
[Index(nameof(RoomSessionId), IsUnique = true, Name = "uq_room_gift_recipient_session")]
public class RoomGiftRecipient
{
    [Key, Column("id")]
    public Guid Id { get; set; }
    [Column("room_session_id")]
    public Guid RoomSessionId { get; set; }
    [Column("recipient_user_id")]
    public Guid RecipientUserId { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }
    [ForeignKey(nameof(RecipientUserId))]
    public User Recipient { get; set; } = null!;
}
