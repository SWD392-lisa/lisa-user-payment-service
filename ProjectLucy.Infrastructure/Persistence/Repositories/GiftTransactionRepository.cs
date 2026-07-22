using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class GiftTransactionRepository : IGiftTransactionRepository
{
    private readonly NeonDbContext _context;

    public GiftTransactionRepository(NeonDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(GiftTransaction txn, CancellationToken ct = default)
        => _context.GiftTransactions.AddAsync(txn, ct).AsTask();

    public Task<GiftTransaction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken ct = default)
        => _context.GiftTransactions.AsNoTracking()
            .Include(t => t.Gift).Include(t => t.Sender).Include(t => t.Receiver)
            .SingleOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);

    public Task<IReadOnlyList<GiftTransaction>> GetBySessionAsync(Guid roomSessionId, CancellationToken ct = default)
        => _context.GiftTransactions
            .AsNoTracking()
            .Include(t => t.Gift)
            .Include(t => t.Sender)
            .Include(t => t.Receiver)
            .Where(t => t.RoomSessionId == roomSessionId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<GiftTransaction>)t.Result, ct);

    public Task<IReadOnlyList<GiftTransaction>> GetBySenderAsync(Guid senderId, CancellationToken ct = default)
        => _context.GiftTransactions
            .AsNoTracking()
            .Include(t => t.Gift)
            .Include(t => t.Receiver)
            .Where(t => t.SenderId == senderId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<GiftTransaction>)t.Result, ct);

    public Task<IReadOnlyList<GiftTransaction>> GetByReceiverAsync(Guid receiverId, CancellationToken ct = default)
        => _context.GiftTransactions
            .AsNoTracking()
            .Include(t => t.Gift)
            .Include(t => t.Sender)
            .Where(t => t.ReceiverId == receiverId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<GiftTransaction>)t.Result, ct);
}
