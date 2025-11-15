using Microsoft.EntityFrameworkCore;
using WeaponShop.Domain;

namespace WeaponShop.Infrastructure;

public static class AppDbContextSeed
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        // Only seed when there are no weapons yet.
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
}
