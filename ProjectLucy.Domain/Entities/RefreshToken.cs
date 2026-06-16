using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectLucy.Domain.Entities;

[Table("refresh_token")]
[Index("ExpiredAt", Name = "idx_refresh_token_expired")]
[Index("IsRevoked", Name = "idx_refresh_token_revoked")]
[Index("UserId", Name = "idx_refresh_token_user_id")]
[Index("Token", Name = "refresh_token_token_key", IsUnique = true)]
public partial class RefreshToken
{
    [Key]
    [Column("token_id")]
    public Guid TokenId { get; set; }

    [Column("token")]
    public string Token { get; set; } = null!;

    [Column("expired_at")]
    public DateTime ExpiredAt { get; set; }

    [Column("is_revoked")]
    public bool? IsRevoked { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("RefreshTokens")]
    public virtual User User { get; set; } = null!;
}
