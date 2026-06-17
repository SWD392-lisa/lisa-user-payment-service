using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectLucy.Application.Interfaces;
using ProjectLucy.Application.Settings;
using ProjectLucy.Domain.Interfaces;
using ProjectLucy.Infrastructure.Authentication;
using ProjectLucy.Infrastructure.Payment;
using ProjectLucy.Infrastructure.Persistence;
using ProjectLucy.Infrastructure.Persistence.Repositories;

namespace ProjectLucy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NeonDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<SePayOptions>(configuration.GetSection(SePayOptions.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ISePayService, SePayService>();

        return services;
    }
}
