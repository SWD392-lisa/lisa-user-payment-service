using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly NeondbContext _context;

    public RoleRepository(NeondbContext context)
    {
        _context = context;
    }

    public Task<Role?> GetByCodeAsync(string roleCode, CancellationToken ct = default)
        => _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.RoleCode == roleCode, ct);

    public Task<Role?> GetByIdAsync(int roleId, CancellationToken ct = default)
        => _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.RoleId == roleId, ct);
}
