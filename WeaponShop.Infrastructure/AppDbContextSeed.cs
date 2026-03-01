using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeaponShop.Domain;
using WeaponShop.Domain.Identity;

namespace WeaponShop.Infrastructure;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetService<IConfiguration>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager, configuration);

        // Only seed weapons when there are no records yet.
        if (await context.Weapons.AnyAsync(cancellationToken))
        {
            return;
        }

        var demoWeapons = new[]
        {
            new Weapon
            {
                Name = "Glock 17",
                Category = "B",
                Description = "Popular 9x19mm semi-automatic pistol.",
                Price = 650m,
                Manufacturer = "Glock"
            },
            new Weapon
            {
                Name = "CZ P-10",
                Category = "B",
                Description = "Modern polymer-framed striker-fired pistol.",
                Price = 700m,
                Manufacturer = "Česká zbrojovka"
            },
            new Weapon
            {
                Name = "Sa vz. 58",
                Category = "B",
                Description = "Classic Czech 7.62×39mm assault rifle (civilian semi-auto version).",
                Price = 1200m,
                Manufacturer = "Česká zbrojovka"
            }
        };

        await context.Weapons.AddRangeAsync(demoWeapons, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { "Admin", "Customer" };
        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration? configuration)
    {
        var adminEmail = configuration?["SeedAdmin:Email"] ?? "admin@weaponshop.local";
        var adminPassword = configuration?["SeedAdmin:Password"] ?? "Admin123!";
        var adminFirstName = configuration?["SeedAdmin:FirstName"] ?? "System";
        var adminLastName = configuration?["SeedAdmin:LastName"] ?? "Admin";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = adminFirstName,
                LastName = adminLastName
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create default admin user '{adminEmail}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign role 'Admin' to '{adminEmail}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
