using ProjectLucy.Domain.Entities;

namespace ProjectLucy.Domain.Interfaces;

public interface IGiftTransactionRepository
{
    Task AddAsync(GiftTransaction txn, CancellationToken ct = default);
    Task<GiftTransaction?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken ct = default);
    Task<IReadOnlyList<GiftTransaction>> GetBySessionAsync(Guid roomSessionId, CancellationToken ct = default);
    Task<IReadOnlyList<GiftTransaction>> GetBySenderAsync(Guid senderId, CancellationToken ct = default);
    Task<IReadOnlyList<GiftTransaction>> GetByReceiverAsync(Guid receiverId, CancellationToken ct = default);
}
