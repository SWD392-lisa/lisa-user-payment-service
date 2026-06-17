using ProjectLucy.Domain.Entities;

namespace ProjectLucy.Domain.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Wallet wallet, CancellationToken ct = default);
}
