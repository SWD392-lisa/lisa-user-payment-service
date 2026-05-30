using ProjectLucy.Domain.Entities;

namespace ProjectLucy.Domain.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByCodeAsync(string roleCode, CancellationToken ct = default);
    Task<Role?> GetByIdAsync(int roleId, CancellationToken ct = default);
}
