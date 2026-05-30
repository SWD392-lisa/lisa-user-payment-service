namespace ProjectLucy.Domain.Entities;

public class RefreshToken
{
    public Guid TokenId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiredAt { get; set; }
    public bool? IsRevoked { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Guid UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
