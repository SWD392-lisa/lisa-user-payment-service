using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

[Table("anonymous_room_identity")]
[Index(nameof(UserId), nameof(RoomSessionId), IsUnique = true, Name = "uq_anonymous_room_identity_user_session")]
[Index(nameof(AnonymousId), IsUnique = true, Name = "uq_anonymous_room_identity_alias")]
public class AnonymousRoomIdentity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("room_session_id")]
    public Guid RoomSessionId { get; set; }

    [Column("anonymous_id")]
    public Guid AnonymousId { get; set; }

    [Column("display_name")]
    [StringLength(80)]
    public string DisplayName { get; set; } = null!;

    [Column("persona_code")]
    [StringLength(40)]
    public string PersonaCode { get; set; } = null!;

    [Column("persona_asset_url")]
    [StringLength(255)]
    public string PersonaAssetUrl { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
