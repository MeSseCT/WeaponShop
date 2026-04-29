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
        try
        {
            var user = User.Identity?.IsAuthenticated == true
                ? await _userManager.GetUserAsync(User)
                : null;
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Skladnik") || User.IsInRole("Zbrojir");
            var isCustomer = User.IsInRole("Customer");

            var isAdultConfirmed = WeaponCategoryPolicy.HasRequiredAge(user);
            var hasVerificationDocuments = user is not null
                && (!string.IsNullOrWhiteSpace(user.IdCardFileName)
                    || !string.IsNullOrWhiteSpace(user.PurchasePermitFileName)
                    || user.FirearmsLicenseRecorded);
            var canViewRestrictedCatalog = WeaponCategoryPolicy.CanBrowseRestrictedCatalog(user, isStaff);

            var allAccessoryProducts = (await _accessoryService.GetAllAsync(cancellationToken))
                .Select(MapAccessory)
                .ToList();
            var allRestrictedProducts = (await _weaponService.GetAllAsync(cancellationToken))
                .Where(weapon => WeaponCategoryPolicy.IsSellableCategory(weapon.Category))
                .Select(weapon => MapWeapon(weapon, user, isStaff, isCustomer))
                .ToList();

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
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Katalog se teď nepodařilo načíst.";
            return View(new StoreCatalogViewModel
            {
                Search = search?.Trim() ?? string.Empty,
                AccessFilter = string.IsNullOrWhiteSpace(access) ? "all" : access.Trim().ToLowerInvariant(),
                AccessoryCategoryFilter = accessoryCategory?.Trim() ?? string.Empty,
                LegalCategoryFilter = legalCategory?.Trim().ToUpperInvariant() ?? string.Empty,
                ManufacturerFilter = manufacturer?.Trim() ?? string.Empty,
                SortBy = string.IsNullOrWhiteSpace(sort) ? "featured" : sort.Trim().ToLowerInvariant(),
                IsAuthenticated = User.Identity?.IsAuthenticated == true
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        try
        {
            var weapon = await _weaponService.GetByIdAsync(id, cancellationToken);
            if (weapon is null)
            {
                return NotFound();
            }

            var user = User.Identity?.IsAuthenticated == true
                ? await _userManager.GetUserAsync(User)
                : null;
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Skladnik") || User.IsInRole("Zbrojir");
            var access = WeaponCategoryPolicy.EvaluateAccess(user, weapon.Category, isStaff);

            if (!access.CanViewDetails)
            {
                TempData["ErrorMessage"] = access.RestrictionMessage;
                return RedirectToAction(nameof(Index), new { access = "restricted" });
            }

            var relatedProducts = (await _weaponService.GetAllAsync(cancellationToken))
                .Where(candidate => candidate.Id != weapon.Id
                    && candidate.Category == weapon.Category
                    && WeaponCategoryPolicy.IsSellableCategory(candidate.Category))
                .Take(3)
                .Select(candidate => MapWeapon(candidate, user, isStaff, User.IsInRole("Customer")))
                .ToList();

            var imagePaths = ResolveWeaponImagePaths(weapon);

            var model = new WeaponDetailsViewModel
            {
                Weapon = weapon,
                ImagePaths = imagePaths,
                ImagePath = imagePaths.First(),
                CanAddToCart = User.IsInRole("Customer") && weapon.IsAvailable && weapon.StockQuantity > 0 && access.CanAddToCart,
                CanViewRestrictedCatalog = WeaponCategoryPolicy.CanBrowseRestrictedCatalog(user, isStaff),
                AccessLabel = access.AccessLabel,
                RestrictionMessage = access.RestrictionMessage,
                RelatedProducts = relatedProducts
            };

            return View(model);
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Detail zbraně se teď nepodařilo načíst.";
            return RedirectToAction(nameof(Index), new { access = "restricted" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> AccessoryDetails(int id, CancellationToken cancellationToken)
    {
        try
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

            var imagePaths = ResolveAccessoryImagePaths(accessory);

            var model = new AccessoryDetailsViewModel
            {
                Accessory = accessory,
                ImagePaths = imagePaths,
                ImagePath = imagePaths.First(),
                RelatedProducts = relatedProducts
            };

            return View(model);
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Detail doplňku se teď nepodařilo načíst.";
            return RedirectToAction(nameof(Index), new { access = "accessories" });
        }
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

    private static StoreCatalogItemViewModel MapWeapon(
        Weapon weapon,
        ApplicationUser? user,
        bool isStaff,
        bool isCustomer)
    {
        var access = WeaponCategoryPolicy.EvaluateAccess(user, weapon.Category, isStaff);

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
            AccessLabel = CatalogPresentation.GetWeaponAccessLabel(weapon.Category),
            RestrictionMessage = access.RestrictionMessage,
            ImagePath = ResolveWeaponImagePath(weapon),
            AccentClass = "accent-restricted",
            CanViewDetails = access.CanViewDetails,
            CanAddToCart = isCustomer && weapon.IsAvailable && weapon.StockQuantity > 0 && access.CanAddToCart
        };
    }

    private static string ResolveWeaponImagePath(Weapon weapon)
    {
        return ResolveWeaponImagePaths(weapon).First();
    }

    private static IReadOnlyList<string> ResolveWeaponImagePaths(Weapon weapon)
    {
        var imagePaths = new List<string>();

        var uploadedImagePath = CatalogImageStorage.ToPublicPath(weapon.ImageFileName);
        if (!string.IsNullOrWhiteSpace(uploadedImagePath))
        {
            imagePaths.Add(uploadedImagePath);
        }

        foreach (var galleryImage in weapon.Images
                     .OrderBy(image => image.SortOrder)
                     .ThenBy(image => image.Id))
        {
            var imagePath = CatalogImageStorage.ToPublicPath(galleryImage.FileName);
            if (!string.IsNullOrWhiteSpace(imagePath) && !imagePaths.Contains(imagePath, StringComparer.OrdinalIgnoreCase))
            {
                imagePaths.Add(imagePath);
            }
        }

        if (imagePaths.Count > 0)
        {
            return imagePaths;
        }

        var name = weapon.Name.ToLowerInvariant();
        if (name.Contains("glock") || name.Contains("p-10") || name.Contains("pistol"))
        {
            return new[] { "/images/catalog/weapon-pistol.svg" };
        }

        if (name.Contains("vz") || name.Contains("rifle") || name.Contains("carbine"))
        {
            return new[] { "/images/catalog/weapon-rifle.svg" };
        }

        return new[] { "/images/catalog/weapon-generic.svg" };
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
            AccentClass = "accent-public",
            CanViewDetails = true,
            CanAddToCart = accessory.IsAvailable && accessory.StockQuantity > 0
        };
    }

    private static string ResolveAccessoryImagePath(Accessory accessory)
    {
        return ResolveAccessoryImagePaths(accessory).First();
    }

    private static IReadOnlyList<string> ResolveAccessoryImagePaths(Accessory accessory)
    {
        var imagePaths = new List<string>();

        var uploadedImagePath = CatalogImageStorage.ToPublicPath(accessory.ImageFileName);
        if (!string.IsNullOrWhiteSpace(uploadedImagePath))
        {
            imagePaths.Add(uploadedImagePath);
        }

        foreach (var galleryImage in accessory.Images
                     .OrderBy(image => image.SortOrder)
                     .ThenBy(image => image.Id))
        {
            var imagePath = CatalogImageStorage.ToPublicPath(galleryImage.FileName);
            if (!string.IsNullOrWhiteSpace(imagePath) && !imagePaths.Contains(imagePath, StringComparer.OrdinalIgnoreCase))
            {
                imagePaths.Add(imagePath);
            }
        }

        if (imagePaths.Count > 0)
        {
            return imagePaths;
        }

        var category = accessory.Category.ToLowerInvariant();
        var name = accessory.Name.ToLowerInvariant();

        if (category.Contains("optic") || name.Contains("dot") || name.Contains("scope"))
        {
            return new[] { "/images/catalog/accessory-optic.svg" };
        }

        if (category.Contains("storage") || name.Contains("safe") || name.Contains("case"))
        {
            return new[] { "/images/catalog/accessory-safe.svg" };
        }

        return new[] { "/images/catalog/accessory-gear.svg" };
    }
}
