using ProjectLucy.Domain.Entities;

namespace ProjectLucy.Domain.Interfaces;

public interface IRoleUpgradeOrderRepository
{
    Task AddAsync(RoleUpgradeOrder order, CancellationToken ct = default);
    Task<RoleUpgradeOrder?> GetByTransactionIdAsync(long transactionId, CancellationToken ct = default);
    Task<RoleUpgradeOrder?> GetByTransactionIdTrackedAsync(long transactionId, CancellationToken ct = default);
    Task<bool> HasActiveOrderForRoleAsync(Guid userId, int toRoleId, CancellationToken ct = default);
}
