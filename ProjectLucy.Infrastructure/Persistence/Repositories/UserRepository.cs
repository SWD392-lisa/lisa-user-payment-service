using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly NeondbContext _context;

    public UserRepository(NeondbContext context)
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
            .FirstOrDefaultAsync(user => user.UserId == userId, ct);

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
}
