using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class RoleUpgradeOrderRepository : IRoleUpgradeOrderRepository
{
    private readonly NeonDbContext _context;

    public RoleUpgradeOrderRepository(NeonDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RoleUpgradeOrder order, CancellationToken ct = default)
        => await _context.RoleUpgradeOrders.AddAsync(order, ct);

    public Task<RoleUpgradeOrder?> GetByTransactionIdAsync(long transactionId, CancellationToken ct = default)
        => _context.RoleUpgradeOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.TransactionId == transactionId, ct);

    public Task<RoleUpgradeOrder?> GetByTransactionIdTrackedAsync(long transactionId, CancellationToken ct = default)
        => _context.RoleUpgradeOrders
            .FirstOrDefaultAsync(o => o.TransactionId == transactionId, ct);

    public Task<bool> HasActiveOrderForRoleAsync(Guid userId, int toRoleId, CancellationToken ct = default)
        => _context.RoleUpgradeOrders
            .AsNoTracking()
            .AnyAsync(o => o.UserId == userId
                && o.ToRoleId == toRoleId
                && o.ActivatedAt != null
                && o.CancelledAt == null, ct);
}
