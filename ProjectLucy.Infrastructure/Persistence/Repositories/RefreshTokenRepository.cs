using Microsoft.EntityFrameworkCore;
using ProjectLucy.Domain.Entities;
using ProjectLucy.Domain.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly NeonDbContext _context;

    public RefreshTokenRepository(NeonDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => _context.RefreshTokens.FirstOrDefaultAsync(refreshToken => refreshToken.Token == token, ct);

    public Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
        => _context.RefreshTokens.AddAsync(refreshToken, ct).AsTask();

    public Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        _context.RefreshTokens.Update(refreshToken);
        return Task.CompletedTask;
    }
}
