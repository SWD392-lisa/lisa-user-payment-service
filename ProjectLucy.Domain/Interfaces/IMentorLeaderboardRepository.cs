using ProjectLucy.Domain.Models;

namespace ProjectLucy.Domain.Interfaces;

public interface IMentorLeaderboardRepository
{
    Task<IReadOnlyList<MentorLeaderboardAggregate>> GetTopMentorsAsync(
        DateTime? periodStartUtc,
        int limit,
        CancellationToken ct = default);
}
