using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain.Identity;
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
                               ?? "Host=localhost;Port=5432;Database=weaponshop;Username=weaponshop;Password=weaponshop_dev_password;Include Error Detail=true";

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IWeaponRepository, WeaponRepository>();
        services.AddScoped<IAccessoryRepository, AccessoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IEmailSender, Services.SmtpEmailSender>();

        return services;
    }
}
