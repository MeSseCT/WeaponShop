using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeaponShop.Application.Interfaces;
using WeaponShop.Infrastructure.Repositories;

namespace WeaponShop.Infrastructure;

/// <summary>
/// Infrastructure-layer dependency injection helpers.
/// Responsible for wiring EF Core and repository implementations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=weaponshop.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IWeaponRepository, WeaponRepository>();

        return services;
    }
}

