using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly NeonDbContext _context;

    public TransactionRepository(NeonDbContext context)
    {
        _context = context;
    }

    public Task<Transaction?> GetByReferenceCodeAsync(string referenceCode, CancellationToken ct = default)
        => _context.Transactions
            .FirstOrDefaultAsync(t => t.ReferenceCode == referenceCode, ct);

    public Task<bool> ExistsByReferenceCodeAsync(string referenceCode, CancellationToken ct = default)
        => _context.Transactions
            .AsNoTracking()
            .AnyAsync(t => t.ReferenceCode == referenceCode, ct);

    public async Task<IReadOnlyList<Transaction>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.Transactions
            .AsNoTracking()
            .Include(t => t.TransactionType)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<int?> GetTypeIdByCodeAsync(string code, CancellationToken ct = default)
    {
        var type = await _context.TransactionTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == code, ct);
        return type?.Id;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
        => await _context.Transactions.AddAsync(transaction, ct);
}
