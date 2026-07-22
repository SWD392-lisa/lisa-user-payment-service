using MediatR;
using ProjectLucy.Application.Common;
using ProjectLucy.Application.DTOs.LeaderboardDtos;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Application.Leaderboard.Queries.GetMentorLeaderboard;

public sealed class GetMentorLeaderboardQueryHandler
    : IRequestHandler<GetMentorLeaderboardQuery, Result<MentorLeaderboardResponseDto>>
{
    private const int LeaderboardLimit = 100;
    private static readonly TimeSpan BangkokOffset = TimeSpan.FromHours(7);
    private readonly IMentorLeaderboardRepository _repository;
    private readonly TimeProvider _timeProvider;

    public GetMentorLeaderboardQueryHandler(
        IMentorLeaderboardRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MentorLeaderboardResponseDto>> Handle(
        GetMentorLeaderboardQuery request,
        CancellationToken ct)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var periodStart = request.Period == "weekly"
            ? GetBangkokWeekStartUtc(nowUtc)
            : (DateTimeOffset?)null;

        var aggregates = await _repository.GetTopMentorsAsync(
            periodStart?.UtcDateTime,
            LeaderboardLimit,
            ct);

        var ranked = aggregates
            .OrderByDescending(entry => entry.TotalGiftValue)
            .ThenByDescending(entry => entry.GiftCount)
            .ThenByDescending(entry => entry.LatestGiftAt)
            .ThenBy(entry => entry.MentorId)
            .Take(LeaderboardLimit)
            .Select((entry, index) => new MentorLeaderboardEntryDto
        {
            Rank = index + 1,
            MentorId = entry.MentorId,
            DisplayName = entry.DisplayName,
            RoleCode = entry.RoleCode.ToUpperInvariant(),
            GiftCount = entry.GiftCount
        }).ToList();

        var items = ranked
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var response = new MentorLeaderboardResponseDto
        {
            Period = request.Period,
            PeriodStart = periodStart?.UtcDateTime,
            PeriodEnd = nowUtc.UtcDateTime,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = ranked.Count,
            Viewer = ranked.FirstOrDefault(entry => entry.MentorId == request.ViewerId),
            Items = items
        };

        return Result<MentorLeaderboardResponseDto>.Success(
            response,
            "Mentor leaderboard retrieved");
    }

    internal static DateTimeOffset GetBangkokWeekStartUtc(DateTimeOffset utcNow)
    {
        var bangkokNow = utcNow.ToOffset(BangkokOffset);
        var daysSinceMonday = ((int)bangkokNow.DayOfWeek + 6) % 7;
        var monday = bangkokNow.Date.AddDays(-daysSinceMonday);
        return new DateTimeOffset(monday, BangkokOffset).ToUniversalTime();
    }
}
