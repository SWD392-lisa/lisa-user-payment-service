namespace ProjectLucy.Application.Interfaces;

/// <summary>
/// Abstracts the database transaction boundary.
/// Implemented in Infrastructure; used by Application command handlers.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
