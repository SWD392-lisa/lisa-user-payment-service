using ProjectLucy.Domain.Entities;

namespace ProjectLucy.Domain.Interfaces;

public interface ITransactionRepository
{
    /// <summary>Tracked lookup by gateway invoice number (reference_code) for updates.</summary>
    Task<Transaction?> GetByReferenceCodeAsync(string referenceCode, CancellationToken ct = default);

    /// <summary>Read-only existence check used during validation.</summary>
    Task<bool> ExistsByReferenceCodeAsync(string referenceCode, CancellationToken ct = default);

    /// <summary>Payment history for a single user, newest first, with type info included.</summary>
    Task<IReadOnlyList<Transaction>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Resolve a transaction_type id from its code (e.g. ONLINE_SEPAY).</summary>
    Task<int?> GetTypeIdByCodeAsync(string code, CancellationToken ct = default);

    Task AddAsync(Transaction transaction, CancellationToken ct = default);
}
