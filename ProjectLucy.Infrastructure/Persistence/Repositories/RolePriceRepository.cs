using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class RolePriceRepository : IRolePriceRepository
{
    private readonly NeonDbContext _context;

    public RolePriceRepository(NeonDbContext context)
    {
        _context = context;
    }

    public Task<List<RolePrice>> GetActiveAsync(CancellationToken ct = default)
        => _context.RolePrices
            .AsNoTracking()
            .Include(rp => rp.Role)
            .Where(rp => rp.IsActive == true)
            .ToListAsync(ct);

    public Task<RolePrice?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.RolePrices
            .AsNoTracking()
            .Include(rp => rp.Role)
            .FirstOrDefaultAsync(rp => rp.Id == id, ct);
}
