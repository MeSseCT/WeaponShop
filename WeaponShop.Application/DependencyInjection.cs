using Microsoft.Extensions.DependencyInjection;
using WeaponShop.Application.Services;

namespace WeaponShop.Application;

/// <summary>
/// Application-layer dependency injection helpers.
/// Keeps the composition root (Program.cs) clean.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IWeaponService, WeaponService>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}
