using ProjectLucy.Domain.Entities;

namespace ProjectLucy.Domain.Interfaces;

public interface IRolePriceRepository
{
    Task<List<RolePrice>> GetActiveAsync(CancellationToken ct = default);
    Task<RolePrice?> GetByIdAsync(int id, CancellationToken ct = default);
}
