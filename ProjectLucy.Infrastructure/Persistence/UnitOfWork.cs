using ProjectLucy.Application.Interfaces;

namespace ProjectLucy.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly NeonDbContext _context;

    public UnitOfWork(NeonDbContext context)
    {
        _context = context;
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
