using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class GiftCatalogRepository : IGiftCatalogRepository
{
    private readonly NeonDbContext _context;

    public GiftCatalogRepository(NeonDbContext context)
    {
        _context = context;
    }

    public Task<IReadOnlyList<GiftCatalog>> GetActiveAsync(CancellationToken ct = default)
        => _context.GiftCatalogs
            .AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<GiftCatalog>)t.Result, ct);

    public Task<GiftCatalog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.GiftCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task AddAsync(GiftCatalog gift, CancellationToken ct = default)
        => _context.GiftCatalogs.AddAsync(gift, ct).AsTask();
}
