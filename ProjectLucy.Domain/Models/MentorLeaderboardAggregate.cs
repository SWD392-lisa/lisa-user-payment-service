namespace ProjectLucy.Domain.Models;

public sealed class MentorLeaderboardAggregate
{
    public Guid MentorId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string RoleCode { get; init; } = string.Empty;
    public decimal TotalGiftValue { get; init; }
    public long GiftCount { get; init; }
    public DateTime LatestGiftAt { get; init; }
}
