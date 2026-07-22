using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Domain.Models;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public sealed class MentorLeaderboardRepository : IMentorLeaderboardRepository
{
    private static readonly string[] EligibleRoleCodes = ["PRO", "SUPER"];
    private readonly NeonDbContext _context;

    public MentorLeaderboardRepository(NeonDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MentorLeaderboardAggregate>> GetTopMentorsAsync(
        DateTime? periodStartUtc,
        int limit,
        CancellationToken ct = default)
    {
        var query = _context.GiftTransactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.Receiver.IsActive &&
                EligibleRoleCodes.Contains(transaction.Receiver.Role.RoleCode.ToUpper()) &&
                transaction.Transaction.Status != null &&
                transaction.Transaction.Status.ToLower() == "completed");

        if (periodStartUtc.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt >= periodStartUtc.Value);
        }

        return await query
            .GroupBy(transaction => new
            {
                transaction.ReceiverId,
                transaction.Receiver.UserFullName,
                transaction.Receiver.Role.RoleCode
            })
            .Select(group => new MentorLeaderboardAggregate
            {
                MentorId = group.Key.ReceiverId,
                DisplayName = group.Key.UserFullName,
                RoleCode = group.Key.RoleCode,
                TotalGiftValue = group.Sum(transaction => transaction.TotalValue),
                GiftCount = group.Sum(transaction => (long)transaction.Quantity),
                LatestGiftAt = group.Max(transaction => transaction.CreatedAt)
            })
            .OrderByDescending(entry => entry.TotalGiftValue)
            .ThenByDescending(entry => entry.GiftCount)
            .ThenByDescending(entry => entry.LatestGiftAt)
            .ThenBy(entry => entry.MentorId)
            .Take(limit)
            .ToListAsync(ct);
    }
}
