using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("WeaponShop.Infrastructure.Seed");

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager, configuration, logger);
        await SeedStaffUserAsync(userManager, configuration, logger, "Skladnik", "warehouse@weaponshop.local", "Skladový", "pracovník", "SeedWarehouse");
        await SeedStaffUserAsync(userManager, configuration, logger, "Zbrojir", "gunsmith@weaponshop.local", "Zbrojířský", "pracovník", "SeedGunsmith");

        await SeedWeaponsAsync(context, cancellationToken);
        await SeedAccessoriesAsync(context, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedWeaponsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var demoWeapons = new[]
        {
            new Weapon
            {
                Name = "Glock 17 Gen5",
                TypeDesignation = "Samonabíjecí pistole",
                Category = "B",
                Description = "Služební a sportovní pistole s polymerovým rámem v ráži 9x19 mm. Vhodná pro každodenní trénink i profesionální použití.",
                Price = 18990m,
                Manufacturer = "Glock",
                Caliber = "9x19 mm",
                PrimarySerialNumber = "GLK17G5-24001",
                StockQuantity = 5,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "CZ Shadow 2",
                TypeDesignation = "Sportovní pistole",
                Category = "B",
                Description = "Celokovová sportovní pistole určená pro dynamické disciplíny a přesnou střelbu na střelnici.",
                Price = 34990m,
                Manufacturer = "Česká zbrojovka",
                Caliber = "9x19 mm",
                PrimarySerialNumber = "CZSH2-24002",
                StockQuantity = 3,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "CZ P-10 C OR",
                TypeDesignation = "Kompaktní pistole",
                Category = "B",
                Description = "Kompaktní pistole připravená pro montáž kolimátoru. Hodí se pro služební nošení i civilní obranu.",
                Price = 16990m,
                Manufacturer = "Česká zbrojovka",
                Caliber = "9x19 mm",
                PrimarySerialNumber = "CZP10C-24003",
                StockQuantity = 4,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "Walther PDP Compact",
                TypeDesignation = "Kompaktní pistole",
                Category = "B",
                Description = "Moderní pistole s ergonomickým úchopem, vhodná pro sport, obranu a střelce, kteří chtějí optickou přípravu.",
                Price = 18490m,
                Manufacturer = "Walther",
                Caliber = "9x19 mm",
                PrimarySerialNumber = "WLPDP-24004",
                StockQuantity = 0,
                IsAvailable = false
            },
            new Weapon
            {
                Name = "Beretta 92X Performance",
                TypeDesignation = "Celokovová sportovní pistole",
                Category = "B",
                Description = "Těžká celokovová pistole zaměřená na sportovní použití a stabilní chování při rychlé střelbě.",
                Price = 42990m,
                Manufacturer = "Beretta",
                Caliber = "9x19 mm",
                PrimarySerialNumber = "BRT92X-24005",
                StockQuantity = 2,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "Smith & Wesson 686 6\"",
                TypeDesignation = "Revolver",
                Category = "B",
                Description = "Klasický revolver v nerezovém provedení pro sportovní střelbu a sběratelské využití.",
                Price = 28990m,
                Manufacturer = "Smith & Wesson",
                Caliber = ".357 Magnum",
                PrimarySerialNumber = "SW686-24006",
                StockQuantity = 1,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "Benelli M4 Tactical",
                TypeDesignation = "Samonabíjecí brokovnice",
                Category = "B",
                Description = "Samonabíjecí brokovnice pro sportovní a taktickou střelbu s důrazem na spolehlivost a rychlou obsluhu.",
                Price = 54990m,
                Manufacturer = "Benelli",
                Caliber = "12/76",
                PrimarySerialNumber = "BNLM4-24007",
                StockQuantity = 1,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "Mossberg 590A1",
                TypeDesignation = "Pumpovací brokovnice",
                Category = "C",
                Description = "Robustní pumpovací brokovnice vhodná pro střelnici, výcvik a profesionální nasazení.",
                Price = 24500m,
                Manufacturer = "Mossberg",
                Caliber = "12/76",
                PrimarySerialNumber = "MS590-24008",
                StockQuantity = 0,
                IsAvailable = false
            },
            new Weapon
            {
                Name = "Tikka T3x Lite",
                TypeDesignation = "Opakovací kulovnice",
                Category = "C",
                Description = "Lehká kulovnice s přesnou hlavní a kvalitním chodem závěru pro myslivost a přesnou střelbu.",
                Price = 31990m,
                Manufacturer = "Tikka",
                Caliber = ".308 Win",
                PrimarySerialNumber = "TKT3X-24009",
                StockQuantity = 3,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "Savage Axis II XP",
                TypeDesignation = "Opakovací kulovnice",
                Category = "C",
                Description = "Základní opakovací kulovnice s montovanou optikou pro lov i vstup do světa kulových zbraní.",
                Price = 22490m,
                Manufacturer = "Savage Arms",
                Caliber = ".308 Win",
                PrimarySerialNumber = "SVGAX2-24010",
                StockQuantity = 2,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "CZ 457 Synthetic",
                TypeDesignation = "Malorážka",
                Category = "C",
                Description = "Malorážka se syntetickou pažbou, jednoduchou údržbou a širokým využitím pro trénink i sportovní střelbu.",
                Price = 15990m,
                Manufacturer = "Česká zbrojovka",
                Caliber = ".22 LR",
                PrimarySerialNumber = "CZ457-24011",
                StockQuantity = 6,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "Ruger 10/22 Carbine",
                TypeDesignation = "Samonabíjecí malorážka",
                Category = "C",
                Description = "Oblíbená malorážka s vysokou spolehlivostí a rozsáhlou možností doplňků a úprav.",
                Price = 12990m,
                Manufacturer = "Ruger",
                Caliber = ".22 LR",
                PrimarySerialNumber = "RGR1022-24012",
                StockQuantity = 4,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "Heckler & Koch MR223 A3",
                TypeDesignation = "Samonabíjecí puška",
                Category = "B",
                Description = "Samonabíjecí puška prémiové třídy vhodná pro sportovní střelbu a náročné uživatele.",
                Price = 79990m,
                Manufacturer = "Heckler & Koch",
                Caliber = "5.56x45 mm",
                PrimarySerialNumber = "HKMR223-24013",
                StockQuantity = 0,
                IsAvailable = false
            },
            new Weapon
            {
                Name = "Pedersoli 1858 New Army",
                TypeDesignation = "Černoprachý revolver",
                Category = "D",
                Description = "Historicky laděný černoprachý revolver pro sportovní střelbu, reenactment a sběratelské účely.",
                Price = 17490m,
                Manufacturer = "Davide Pedersoli",
                Caliber = ".44",
                PrimarySerialNumber = "PDR1858-24014",
                StockQuantity = 2,
                IsAvailable = true
            },
            new Weapon
            {
                Name = "Zoraki R1 4,5\"",
                TypeDesignation = "Expanzní revolver",
                Category = "CI",
                Description = "Expanzní revolver určený pro signální a obranné použití v režimu kategorie C-I.",
                Price = 9990m,
                Manufacturer = "Zoraki",
                Caliber = "9 mm R.K.",
                PrimarySerialNumber = "ZRKRI-24015",
                StockQuantity = 5,
                IsAvailable = true
            }
        };

        var existingWeapons = await context.Weapons.ToListAsync(cancellationToken);
        var existingByKey = existingWeapons.ToDictionary(
            item => BuildProductKey(item.Name, item.Manufacturer),
            item => item,
            StringComparer.OrdinalIgnoreCase);

        var weaponsToAdd = new List<Weapon>();
        foreach (var demoWeapon in demoWeapons)
        {
            var key = BuildProductKey(demoWeapon.Name, demoWeapon.Manufacturer);
            if (!existingByKey.TryGetValue(key, out var existingWeapon))
            {
                weaponsToAdd.Add(demoWeapon);
                continue;
            }

            if (string.IsNullOrWhiteSpace(existingWeapon.TypeDesignation))
            {
                existingWeapon.TypeDesignation = demoWeapon.TypeDesignation;
            }

            if (string.IsNullOrWhiteSpace(existingWeapon.Caliber))
            {
                existingWeapon.Caliber = demoWeapon.Caliber;
            }

            if (string.IsNullOrWhiteSpace(existingWeapon.PrimarySerialNumber))
            {
                existingWeapon.PrimarySerialNumber = demoWeapon.PrimarySerialNumber;
            }
        }

        if (weaponsToAdd.Count > 0)
        {
            await context.Weapons.AddRangeAsync(weaponsToAdd, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        await EnsureWeaponUnitsAsync(context, cancellationToken);
    }

    private static async Task SeedAccessoriesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var demoAccessories = new[]
        {
            new Accessory
            {
                Name = "Vector Frenzy 1x20",
                Category = "Optics",
                Description = "Kompaktní kolimátor vhodný pro pistole, PCC platformy a rychlé míření na krátké vzdálenosti.",
                Price = 4890m,
                Manufacturer = "Vector Optics",
                StockQuantity = 12,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Holosun HS407C X2",
                Category = "Optics",
                Description = "Pistolový kolimátor s dlouhou výdrží baterie a odolnou konstrukcí pro každodenní použití.",
                Price = 7990m,
                Manufacturer = "Holosun",
                StockQuantity = 5,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Vortex Crossfire Red Dot",
                Category = "Optics",
                Description = "Univerzální kolimátor pro dlouhé zbraně a sportovní sestavy se snadným nastřelením.",
                Price = 5490m,
                Manufacturer = "Vortex",
                StockQuantity = 8,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Meopta Optika6 3-18x50",
                Category = "Optics",
                Description = "Puškohled pro přesnou střelbu a lov s kvalitním obrazem a dostatečným rozsahem zvětšení.",
                Price = 18490m,
                Manufacturer = "Meopta",
                StockQuantity = 0,
                IsAvailable = false
            },
            new Accessory
            {
                Name = "Dasta OWB Compact",
                Category = "Storage",
                Description = "Jednoduché vnější pouzdro pro kompaktní pistole vhodné pro každodenní nošení i střelnici.",
                Price = 690m,
                Manufacturer = "Dasta",
                StockQuantity = 18,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Ghost Civilian IWB",
                Category = "Storage",
                Description = "Skryté vnitřní pouzdro s pohodlným uchycením pro civilní nošení krátké zbraně.",
                Price = 1290m,
                Manufacturer = "Ghost",
                StockQuantity = 7,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Plano Tactical Hard Case",
                Category = "Storage",
                Description = "Pevný kufr pro přepravu zbraně a optiky s pěnovou výplní a bezpečnostním uzavíráním.",
                Price = 3290m,
                Manufacturer = "Plano",
                StockQuantity = 4,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Gun Safe Compact S1",
                Category = "Storage",
                Description = "Kompaktní schránka pro bezpečné uložení pistole, dokladů a drobného příslušenství.",
                Price = 4590m,
                Manufacturer = "Rottner",
                StockQuantity = 0,
                IsAvailable = false
            },
            new Accessory
            {
                Name = "Savior Urban Rifle Bag 42\"",
                Category = "Storage",
                Description = "Měkký obal pro dlouhou zbraň s kapsami na zásobníky, optiku a základní příslušenství.",
                Price = 2790m,
                Manufacturer = "Savior Equipment",
                StockQuantity = 6,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Peltor SportTac",
                Category = "Protection",
                Description = "Aktivní sluchátka pro střelnici, která tlumí výstřely a současně zachovávají okolní komunikaci.",
                Price = 3890m,
                Manufacturer = "3M Peltor",
                StockQuantity = 10,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Walker's Razor Slim",
                Category = "Protection",
                Description = "Lehká elektronická ochrana sluchu pro sportovní střelbu a delší pobyt na střelnici.",
                Price = 1690m,
                Manufacturer = "Walker's",
                StockQuantity = 14,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Pyramex I-Force Clear",
                Category = "Protection",
                Description = "Uzavřené ochranné brýle s čirým zorníkem pro vnitřní střelnice i servisní práce.",
                Price = 490m,
                Manufacturer = "Pyramex",
                StockQuantity = 32,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Otis Range Cleaning Kit",
                Category = "Protection",
                Description = "Praktická sada pro základní čištění zbraní po střelbě v kompaktním balení.",
                Price = 1490m,
                Manufacturer = "Otis",
                StockQuantity = 9,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Walther ProSecur 16 ml",
                Category = "SelfDefense",
                Description = "Pepřový sprej pro osobní ochranu a každodenní nošení s jednoduchou a rychlou aktivací.",
                Price = 249m,
                Manufacturer = "Walther",
                StockQuantity = 25,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "ESP Teleskopický obušek 21\"",
                Category = "SelfDefense",
                Description = "Profesionální teleskopický obušek z kalené oceli určený pro osobní ochranu a služební použití.",
                Price = 1190m,
                Manufacturer = "ESP",
                StockQuantity = 11,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Guardian Angel Personal Alarm",
                Category = "SelfDefense",
                Description = "Osobní alarm s hlasitou sirénou vhodný pro civilní nošení a krizové situace.",
                Price = 390m,
                Manufacturer = "Guardian Angel",
                StockQuantity = 17,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Piexon JPX Trainer",
                Category = "SelfDefense",
                Description = "Tréninková pomůcka pro nácvik práce s obranným prostředkem bez ostré náplně.",
                Price = 990m,
                Manufacturer = "Piexon",
                StockQuantity = 0,
                IsAvailable = false
            },
            new Accessory
            {
                Name = "Gamo Shadow DX 4,5 mm",
                Category = "Airguns",
                Description = "Základní vzduchovka pro rekreační střelbu a trénink na kratší vzdálenosti.",
                Price = 4390m,
                Manufacturer = "Gamo",
                StockQuantity = 7,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Hatsan Airtact 4,5 mm",
                Category = "Airguns",
                Description = "Lehká vzduchovka s polymerovou pažbou vhodná pro hobby střelce a začátečníky.",
                Price = 3990m,
                Manufacturer = "Hatsan",
                StockQuantity = 5,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Umarex UX Strike Point 4x32",
                Category = "Airguns",
                Description = "Rekreační vzduchovka dodávaná se základní optikou pro nenáročné hobby použití.",
                Price = 5290m,
                Manufacturer = "Umarex",
                StockQuantity = 3,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "JSB Exact Diabolo 4,52 mm",
                Category = "Airguns",
                Description = "Přesné diabolo střelivo pro sportovní i hobby střelbu ze vzduchovek ráže 4,5 mm.",
                Price = 289m,
                Manufacturer = "JSB Match Diabolo",
                StockQuantity = 40,
                IsAvailable = true
            },
            new Accessory
            {
                Name = "Umarex Ruger Air Scout Magnum",
                Category = "Airguns",
                Description = "Výkonnější vzduchovka s puškohledem pro rekreační střelbu a trénink přesnosti.",
                Price = 6990m,
                Manufacturer = "Umarex",
                StockQuantity = 0,
                IsAvailable = false
            }
        };

        var existingKeys = (await context.Accessories
                .Select(item => new { item.Name, item.Manufacturer })
                .ToListAsync(cancellationToken))
            .Select(item => BuildProductKey(item.Name, item.Manufacturer))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var accessoriesToAdd = demoAccessories
            .Where(item => !existingKeys.Contains(BuildProductKey(item.Name, item.Manufacturer)))
            .ToList();

        if (accessoriesToAdd.Count > 0)
        {
            await context.Accessories.AddRangeAsync(accessoriesToAdd, cancellationToken);
        }
    }

    private static string BuildProductKey(string? name, string? manufacturer)
    {
        return $"{name?.Trim().ToLowerInvariant()}|{manufacturer?.Trim().ToLowerInvariant()}";
    }

    private static async Task EnsureWeaponUnitsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var weapons = await context.Weapons
            .Include(weapon => weapon.Units)
            .ThenInclude(unit => unit.Parts)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        foreach (var weapon in weapons)
        {
            var targetCount = Math.Max(weapon.StockQuantity, 0);
            var currentCount = weapon.Units.Count(unit => unit.Status == WeaponUnitStatus.InStock);
            if (currentCount >= targetCount)
            {
                continue;
            }

            for (var index = currentCount; index < targetCount; index++)
            {
                var unitNumber = index + 1;
                var baseSerial = string.IsNullOrWhiteSpace(weapon.PrimarySerialNumber)
                    ? $"WS-{weapon.Id:D4}"
                    : weapon.PrimarySerialNumber.Trim();

                var primarySerial = unitNumber == 1
                    ? baseSerial
                    : $"{baseSerial}-{unitNumber:D2}";

                if (weapon.Units.Any(unit => string.Equals(unit.PrimarySerialNumber, primarySerial, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                weapon.Units.Add(new WeaponUnit
                {
                    PrimarySerialNumber = primarySerial,
                    Status = WeaponUnitStatus.InStock,
                    Parts = new List<WeaponUnitPart>
                    {
                        new()
                        {
                            SlotNumber = 1,
                            PartName = "Hlaveň",
                            SerialNumber = $"{primarySerial}-P1"
                        },
                        new()
                        {
                            SlotNumber = 2,
                            PartName = "Závěr",
                            SerialNumber = $"{primarySerial}-P2"
                        },
                        new()
                        {
                            SlotNumber = 3,
                            PartName = "Rám",
                            SerialNumber = $"{primarySerial}-P3"
                        }
                    }
                });
            }

            weapon.StockQuantity = weapon.Units.Count(unit => unit.Status == WeaponUnitStatus.InStock);
            weapon.IsAvailable = weapon.StockQuantity > 0;
        }
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
                throw new InvalidOperationException($"Nepodařilo se vytvořit roli '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration? configuration,
        ILogger? logger)
    {
        var adminEmail = configuration?["SeedAdmin:Email"] ?? "admin@weaponshop.local";
        var adminPassword = configuration?["SeedAdmin:Password"];
        var adminFirstName = configuration?["SeedAdmin:FirstName"] ?? "System";
        var adminLastName = configuration?["SeedAdmin:LastName"] ?? "Admin";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                logger?.LogWarning(
                    "Přeskakuje se vytvoření výchozího administrátora {Email}, protože není nastaveno SeedAdmin:Password.",
                    adminEmail);
                return;
            }

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
                    $"Nepodařilo se vytvořit výchozího administrátora '{adminEmail}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Nepodařilo se přiřadit roli 'Admin' uživateli '{adminEmail}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    private static async Task SeedStaffUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration? configuration,
        ILogger? logger,
        string roleName,
        string fallbackEmail,
        string fallbackFirstName,
        string fallbackLastName,
        string configSection)
    {
        var email = configuration?[$"{configSection}:Email"] ?? fallbackEmail;
        var password = configuration?[$"{configSection}:Password"];
        var firstName = configuration?[$"{configSection}:FirstName"] ?? fallbackFirstName;
        var lastName = configuration?[$"{configSection}:LastName"] ?? fallbackLastName;

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                logger?.LogWarning(
                    "Přeskakuje se vytvoření výchozího účtu pro roli {RoleName} ({Email}), protože není nastaveno {ConfigSection}:Password.",
                    roleName,
                    email,
                    configSection);
                return;
            }

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
                    $"Nepodařilo se vytvořit výchozí účet pro roli '{roleName}' s e-mailem '{email}': {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var roleResult = await userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Nepodařilo se přiřadit roli '{roleName}' uživateli '{email}': {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
