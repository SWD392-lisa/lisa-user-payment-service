using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly NeonDbContext _context;

    public UserRepository(NeonDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.UserEmail == email, ct);

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default)
        => _context.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.UserId == userId, ct);

    public Task<User?> GetByIdTrackedAsync(Guid userId, CancellationToken ct = default)
        => _context.Users
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.UserId == userId, ct);

    public async Task<(IReadOnlyList<User> Items, int Total)> SearchAsync(
        string? search,
        string? roleCode,
        bool? isActive,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(user =>
                user.UserEmail.ToLower().Contains(term) ||
                user.UserFullName.ToLower().Contains(term) ||
                (user.UserPhoneNumber != null && user.UserPhoneNumber.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            var normalizedRole = roleCode.Trim().ToUpper();
            query = query.Where(user => user.Role.RoleCode == normalizedRole);
        }

        if (isActive.HasValue)
            query = query.Where(user => user.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(user => user.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.UserEmail == email, ct);

    public Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default)
        => _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.UserPhoneNumber == phone, ct);

    public Task AddAsync(User user, CancellationToken ct = default)
        => _context.Users.AddAsync(user, ct).AsTask();

    public Task RevokeTokensAsync(Guid userId, CancellationToken ct = default)
        => _context.RefreshTokens
            .Where(token => token.UserId == userId && (token.IsRevoked == false || token.IsRevoked == null))
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.IsRevoked, true), ct);
}
