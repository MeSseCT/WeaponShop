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
        await SeedStaffUserAsync(userManager, configuration, "Skladnik", "warehouse@weaponshop.local", "Warehouse123!", "Warehouse", "Staff", "SeedWarehouse");
        await SeedStaffUserAsync(userManager, configuration, "Zbrojir", "gunsmith@weaponshop.local", "Gunsmith123!", "Gunsmith", "Staff", "SeedGunsmith");

        if (!await context.Weapons.AnyAsync(cancellationToken))
        {
            var demoWeapons = new[]
            {
                new Weapon
                {
                    Name = "Glock 17",
                    Category = "B",
                    Description = "Popular 9x19mm semi-automatic pistol.",
                    Price = 650m,
                    Manufacturer = "Glock",
                    StockQuantity = 5,
                    IsAvailable = true
                },
                new Weapon
                {
                    Name = "CZ P-10",
                    Category = "B",
                    Description = "Modern polymer-framed striker-fired pistol.",
                    Price = 700m,
                    Manufacturer = "Česká zbrojovka",
                    StockQuantity = 6,
                    IsAvailable = true
                },
                new Weapon
                {
                    Name = "Sa vz. 58",
                    Category = "B",
                    Description = "Classic Czech 7.62×39mm assault rifle (civilian semi-auto version).",
                    Price = 1200m,
                    Manufacturer = "Česká zbrojovka",
                    StockQuantity = 4,
                    IsAvailable = true
                }
            };

            await context.Weapons.AddRangeAsync(demoWeapons, cancellationToken);
        }

        var demoAccessories = new[]
        {
            new Accessory
            {
                Name = "Vector Red Dot",
                Category = "Optics",
                Description = "Compact red dot for range training, PCC builds and modern sport setups.",
                Price = 189m,
                Manufacturer = "Vector",
                StockQuantity = 14,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Guardian Safe S1",
                Category = "Storage",
                Description = "Home safe for documents, ammunition and shooting accessories.",
                Price = 249m,
                Manufacturer = "Guardian",
                StockQuantity = 6,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Range Pro Kit",
                Category = "Protection",
                Description = "Hearing protection, clear glasses and a compact cleaning kit in one bundle.",
                Price = 119m,
                Manufacturer = "Otis",
                StockQuantity = 18,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Pepper Guard Compact",
                Category = "SelfDefense",
                Description = "Compact pepřový sprej pro osobní ochranu a každodenní nošení.",
                Price = 29m,
                Manufacturer = "Walther",
                StockQuantity = 24,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Umarex Air Rifle 4x32",
                Category = "Airguns",
                Description = "Základní vzduchovka se zaměřovačem pro rekreační střelbu na zahradě i střelnici.",
                Price = 219m,
                Manufacturer = "Umarex",
                StockQuantity = 9,
                IsAvailable = true
            }
        };

        foreach (var accessory in demoAccessories)
        {
            var exists = await context.Accessories
                .AnyAsync(item => item.Name == accessory.Name, cancellationToken);

            if (!exists)
            {
                await context.Accessories.AddAsync(accessory, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { "Admin", "Customer", "Skladnik", "Zbrojir" };
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

    private static async Task SeedStaffUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration? configuration,
        string roleName,
        string fallbackEmail,
        string fallbackPassword,
        string fallbackFirstName,
        string fallbackLastName,
        string configSection)
    {
        var email = configuration?[$"{configSection}:Email"] ?? fallbackEmail;
        var password = configuration?[$"{configSection}:Password"] ?? fallbackPassword;
        var firstName = configuration?[$"{configSection}:FirstName"] ?? fallbackFirstName;
        var lastName = configuration?[$"{configSection}:LastName"] ?? fallbackLastName;

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create default {roleName} user '{email}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var roleResult = await userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign role '{roleName}' to '{email}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
