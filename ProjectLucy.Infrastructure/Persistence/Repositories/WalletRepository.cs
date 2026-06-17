using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly NeonDbContext _context;

    public WalletRepository(NeonDbContext context)
    {
        _context = context;
    }

    public Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);

    public async Task AddAsync(Wallet wallet, CancellationToken ct = default)
        => await _context.Wallets.AddAsync(wallet, ct);
}
