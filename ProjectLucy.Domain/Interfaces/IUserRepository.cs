using ProjectLucy.Domain.Entities;

namespace ProjectLucy.Domain.Interfaces;

/// <summary>
/// Repository contract for User — defined in Domain so Application can depend on it
/// without knowing about EF Core. Implemented in Infrastructure.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}
