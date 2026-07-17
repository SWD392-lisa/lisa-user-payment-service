using ProjectLucy.Domain.Entities;

namespace ProjectLucy.Domain.Interfaces;

public interface IGiftCatalogRepository
{
    Task<IReadOnlyList<GiftCatalog>> GetActiveAsync(CancellationToken ct = default);
    Task<GiftCatalog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(GiftCatalog gift, CancellationToken ct = default);
}
