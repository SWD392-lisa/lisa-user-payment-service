namespace ProjectLucy.Application.DTOs.LeaderboardDtos;

public sealed class MentorLeaderboardEntryDto
{
    public int Rank { get; init; }
    public Guid MentorId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string RoleCode { get; init; } = string.Empty;
    public long GiftCount { get; init; }
}

public sealed class MentorLeaderboardResponseDto
{
    public string Period { get; init; } = string.Empty;
    public DateTime? PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public MentorLeaderboardEntryDto? Viewer { get; init; }
    public IReadOnlyList<MentorLeaderboardEntryDto> Items { get; init; } = [];
}
