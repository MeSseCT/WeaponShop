using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Domain.Identity;
using WeaponShop.Web.Helpers;
using WeaponShop.Web.ViewModels.Weapons;

namespace WeaponShop.Web.Controllers;

public class WeaponsController : Controller
{
    private readonly IAccessoryService _accessoryService;
    private readonly IWeaponService _weaponService;
    private readonly UserManager<ApplicationUser> _userManager;

    public WeaponsController(
        IAccessoryService accessoryService,
        IWeaponService weaponService,
        UserManager<ApplicationUser> userManager)
    {
        _accessoryService = accessoryService;
        _weaponService = weaponService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? access,
        string? accessoryCategory,
        string? legalCategory,
        string? manufacturer,
        string? search,
        string? sort,
        CancellationToken cancellationToken)
    {
        var user = User.Identity?.IsAuthenticated == true
            ? await _userManager.GetUserAsync(User)
            : null;

        var isAdultConfirmed = user?.DateOfBirth is { } dateOfBirth && IsAdult(dateOfBirth);
        var hasVerificationDocuments = user is not null && HasRequiredDocuments(user);
        var canViewRestrictedCatalog = CanViewRestrictedCatalog(user);

        var allAccessoryProducts = (await _accessoryService.GetAllAsync(cancellationToken))
            .Select(MapAccessory)
            .ToList();
        var allRestrictedProducts = (await _weaponService.GetAllAsync(cancellationToken)).Select(MapWeapon).ToList();

        var accessoryProducts = ApplyAccessoryFilters(
                allAccessoryProducts,
                accessoryCategory,
                manufacturer,
                search,
                sort)
            .ToList();

        var restrictedProducts = ApplyRestrictedFilters(
                allRestrictedProducts,
                legalCategory,
                manufacturer,
                search,
                sort)
            .ToList();

        var model = new StoreCatalogViewModel
        {
            Search = search?.Trim() ?? string.Empty,
            AccessFilter = string.IsNullOrWhiteSpace(access) ? "all" : access.Trim().ToLowerInvariant(),
            AccessoryCategoryFilter = accessoryCategory?.Trim() ?? string.Empty,
            LegalCategoryFilter = legalCategory?.Trim().ToUpperInvariant() ?? string.Empty,
            ManufacturerFilter = manufacturer?.Trim() ?? string.Empty,
            SortBy = string.IsNullOrWhiteSpace(sort) ? "featured" : sort.Trim().ToLowerInvariant(),
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
            IsAdultConfirmed = isAdultConfirmed,
            HasVerificationDocuments = hasVerificationDocuments,
            CanViewRestrictedCatalog = canViewRestrictedCatalog,
            Manufacturers = allAccessoryProducts
                .Concat(allRestrictedProducts)
                .Select(product => product.Manufacturer)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList(),
            AccessoryCategories = allAccessoryProducts
                .Select(product => product.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(CatalogPresentation.GetAccessoryCategorySortKey)
                .ThenBy(CatalogPresentation.GetAccessoryCategoryLabel)
                .ToList(),
            LegalCategories = allRestrictedProducts
                .Select(product => product.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(CatalogPresentation.GetWeaponCategorySortKey)
                .ToList(),
            AccessoryProducts = accessoryProducts,
            RestrictedProducts = restrictedProducts,
            RestrictedPreviewProducts = restrictedProducts.Take(3).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(id, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        var user = User.Identity?.IsAuthenticated == true
            ? await _userManager.GetUserAsync(User)
            : null;

        if (!CanViewRestrictedCatalog(user))
        {
            TempData["ErrorMessage"] = "Kategorie zbraní se odemkne až po přihlášení, potvrzení věku 18+ a nahrání dokladů.";
            return RedirectToAction(nameof(Index), new { access = "restricted" });
        }

        var relatedProducts = (await _weaponService.GetAllAsync(cancellationToken))
            .Where(candidate => candidate.Id != weapon.Id && candidate.Category == weapon.Category)
            .Take(3)
            .Select(MapWeapon)
            .ToList();

        var model = new WeaponDetailsViewModel
        {
            Weapon = weapon,
            ImagePath = ResolveWeaponImagePath(weapon),
            CanAddToCart = User.IsInRole("Customer") && weapon.IsAvailable && weapon.StockQuantity > 0,
            CanViewRestrictedCatalog = true,
            RelatedProducts = relatedProducts
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AccessoryDetails(int id, CancellationToken cancellationToken)
    {
        var accessory = await _accessoryService.GetByIdAsync(id, cancellationToken);
        if (accessory is null)
        {
            return NotFound();
        }

        var relatedProducts = (await _accessoryService.GetAllAsync(cancellationToken))
            .Where(candidate => candidate.Id != accessory.Id && candidate.Category == accessory.Category)
            .Take(3)
            .Select(MapAccessory)
            .ToList();

        var model = new AccessoryDetailsViewModel
        {
            Accessory = accessory,
            ImagePath = ResolveAccessoryImagePath(accessory),
            RelatedProducts = relatedProducts
        };

        return View(model);
    }

    private bool CanViewRestrictedCatalog(ApplicationUser? user)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Skladnik") || User.IsInRole("Zbrojir"))
        {
            return true;
        }

        return user is not null
            && user.DateOfBirth is { } dateOfBirth
            && IsAdult(dateOfBirth)
            && HasRequiredDocuments(user);
    }

    private static bool HasRequiredDocuments(ApplicationUser user)
    {
        return !string.IsNullOrWhiteSpace(user.IdCardFileName)
            || !string.IsNullOrWhiteSpace(user.DriverLicenseFileName);
    }

    private static bool IsAdult(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;

        if (today < dateOfBirth.AddYears(age))
        {
            age--;
        }

        return age >= 18;
    }

    private static IEnumerable<StoreCatalogItemViewModel> ApplyAccessoryFilters(
        IReadOnlyList<StoreCatalogItemViewModel> products,
        string? accessoryCategory,
        string? manufacturer,
        string? search,
        string? sort)
    {
        var query = products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(accessoryCategory))
        {
            query = query.Where(product => string.Equals(product.Category, accessoryCategory.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(manufacturer))
        {
            query = query.Where(product => string.Equals(product.Manufacturer, manufacturer.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(product =>
                product.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || product.ShortDescription.Contains(term, StringComparison.OrdinalIgnoreCase)
                || product.CategoryDisplay.Contains(term, StringComparison.OrdinalIgnoreCase)
                || product.Manufacturer.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return SortProducts(query, sort);
    }

    private static IEnumerable<StoreCatalogItemViewModel> ApplyRestrictedFilters(
        IReadOnlyList<StoreCatalogItemViewModel> products,
        string? legalCategory,
        string? manufacturer,
        string? search,
        string? sort)
    {
        var query = products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(legalCategory))
        {
            query = query.Where(product => string.Equals(product.Category, legalCategory.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(manufacturer))
        {
            query = query.Where(product => string.Equals(product.Manufacturer, manufacturer.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(product =>
                product.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || product.ShortDescription.Contains(term, StringComparison.OrdinalIgnoreCase)
                || product.CategoryDisplay.Contains(term, StringComparison.OrdinalIgnoreCase)
                || product.Manufacturer.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return SortProducts(query, sort);
    }

    private static IEnumerable<StoreCatalogItemViewModel> SortProducts(
        IEnumerable<StoreCatalogItemViewModel> products,
        string? sort)
    {
        return (sort ?? "featured").Trim().ToLowerInvariant() switch
        {
            "price-asc" => products.OrderBy(product => product.Price).ThenBy(product => product.Name),
            "price-desc" => products.OrderByDescending(product => product.Price).ThenBy(product => product.Name),
            "stock" => products.OrderByDescending(product => product.StockQuantity ?? 0).ThenBy(product => product.Name),
            _ => products
                .OrderByDescending(product => product.IsAvailable)
                .ThenByDescending(product => product.StockQuantity ?? 0)
                .ThenBy(product => product.Name)
        };
    }

    private static StoreCatalogItemViewModel MapWeapon(Weapon weapon)
    {
        return new StoreCatalogItemViewModel
        {
            WeaponId = weapon.Id,
            Slug = $"weapon-{weapon.Id}",
            Name = weapon.Name,
            Category = weapon.Category,
            CategoryDisplay = CatalogPresentation.GetWeaponCategoryLabel(weapon.Category),
            Manufacturer = weapon.Manufacturer,
            ShortDescription = string.IsNullOrWhiteSpace(weapon.Description)
                ? CatalogPresentation.GetWeaponFallbackDescription()
                : weapon.Description,
            Price = weapon.Price,
            StockQuantity = weapon.StockQuantity,
            IsAvailable = weapon.IsAvailable && weapon.StockQuantity > 0,
            IsRestricted = true,
            AccessLabel = CatalogPresentation.GetWeaponAccessLabel(),
            ImagePath = ResolveWeaponImagePath(weapon),
            AccentClass = "accent-restricted"
        };
    }

    private static string ResolveWeaponImagePath(Weapon weapon)
    {
        var uploadedImagePath = CatalogImageStorage.ToPublicPath(weapon.ImageFileName);
        if (!string.IsNullOrWhiteSpace(uploadedImagePath))
        {
            return uploadedImagePath;
        }

        var name = weapon.Name.ToLowerInvariant();
        if (name.Contains("glock") || name.Contains("p-10") || name.Contains("pistol"))
        {
            return "/images/catalog/weapon-pistol.svg";
        }

        if (name.Contains("vz") || name.Contains("rifle") || name.Contains("carbine"))
        {
            return "/images/catalog/weapon-rifle.svg";
        }

        return "/images/catalog/weapon-generic.svg";
    }

    private static StoreCatalogItemViewModel MapAccessory(Accessory accessory)
    {
        return new StoreCatalogItemViewModel
        {
            AccessoryId = accessory.Id,
            Slug = $"accessory-{accessory.Id}",
            Name = accessory.Name,
            Category = accessory.Category,
            CategoryDisplay = CatalogPresentation.GetAccessoryCategoryLabel(accessory.Category),
            Manufacturer = accessory.Manufacturer,
            ShortDescription = string.IsNullOrWhiteSpace(accessory.Description)
                ? CatalogPresentation.GetAccessoryFallbackDescription()
                : accessory.Description,
            Price = accessory.Price,
            StockQuantity = accessory.StockQuantity,
            IsAvailable = accessory.IsAvailable && accessory.StockQuantity > 0,
            IsRestricted = false,
            AccessLabel = CatalogPresentation.GetAccessoryAccessLabel(),
            ImagePath = ResolveAccessoryImagePath(accessory),
            AccentClass = "accent-public"
        };
    }

    private static string ResolveAccessoryImagePath(Accessory accessory)
    {
        var uploadedImagePath = CatalogImageStorage.ToPublicPath(accessory.ImageFileName);
        if (!string.IsNullOrWhiteSpace(uploadedImagePath))
        {
            return uploadedImagePath;
        }

        var category = accessory.Category.ToLowerInvariant();
        var name = accessory.Name.ToLowerInvariant();

        if (category.Contains("optic") || name.Contains("dot") || name.Contains("scope"))
        {
            return "/images/catalog/accessory-optic.svg";
        }

        if (category.Contains("storage") || name.Contains("safe") || name.Contains("case"))
        {
            return "/images/catalog/accessory-safe.svg";
        }

        return "/images/catalog/accessory-gear.svg";
    }
}
