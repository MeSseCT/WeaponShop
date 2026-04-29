using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Domain.Identity;
using WeaponShop.Web.Helpers;
using WeaponShop.Web.ViewModels.Home;

namespace WeaponShop.Web.Controllers;

public class HomeController : Controller
{
    private readonly IAccessoryService _accessoryService;
    private readonly IWeaponService _weaponService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        IAccessoryService accessoryService,
        IWeaponService weaponService,
        UserManager<ApplicationUser> userManager)
    {
        _accessoryService = accessoryService;
        _weaponService = weaponService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new HomePageViewModel();

        try
        {
            var user = User.Identity?.IsAuthenticated == true
                ? await _userManager.GetUserAsync(User)
                : null;
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Skladnik") || User.IsInRole("Zbrojir");
            var recommendedProducts = new List<HomeRecommendedProductViewModel>();

            recommendedProducts.AddRange((await _accessoryService.GetAllAsync(cancellationToken))
                .Where(item => item.IsAvailable && item.StockQuantity > 0)
                .Select(MapAccessory));

            recommendedProducts.AddRange((await _weaponService.GetAllAsync(cancellationToken))
                .Where(item => item.IsAvailable && item.StockQuantity > 0 && WeaponCategoryPolicy.IsSellableCategory(item.Category))
                .Select(item => MapWeapon(item, user, isStaff)));

            if (recommendedProducts.Count > 0)
            {
                model.RecommendedProduct = recommendedProducts[Random.Shared.Next(recommendedProducts.Count)];
            }
        }
        catch
        {
            // Keep homepage available even if recommendation data cannot be loaded.
        }

        return View(model);
    }

    [Route("/Error")]
    [HttpGet]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        ViewData["OriginalPath"] = exceptionFeature?.Path ?? string.Empty;
        return View();
    }

    private static HomeRecommendedProductViewModel MapAccessory(Accessory accessory)
    {
        return new HomeRecommendedProductViewModel
        {
            Name = accessory.Name,
            Manufacturer = accessory.Manufacturer,
            CategoryDisplay = CatalogPresentation.GetAccessoryCategoryLabel(accessory.Category),
            Price = accessory.Price,
            ImagePath = ResolveAccessoryImagePath(accessory),
            Summary = string.IsNullOrWhiteSpace(accessory.Description)
                ? CatalogPresentation.GetAccessoryFallbackDescription()
                : accessory.Description,
            Badge = "Doplněk",
            BadgeClass = "badge-chip-public",
            ActionLabel = "Zobrazit detail",
            ActionController = "Weapons",
            ActionName = "AccessoryDetails",
            ActionRouteId = accessory.Id
        };
    }

    private static HomeRecommendedProductViewModel MapWeapon(Weapon weapon, ApplicationUser? user, bool isStaff)
    {
        var access = WeaponCategoryPolicy.EvaluateAccess(user, weapon.Category, isStaff);

        return new HomeRecommendedProductViewModel
        {
            Name = weapon.Name,
            Manufacturer = weapon.Manufacturer,
            CategoryDisplay = CatalogPresentation.GetWeaponCategoryLabel(weapon.Category),
            Price = weapon.Price,
            ImagePath = ResolveWeaponImagePath(weapon),
            Summary = string.IsNullOrWhiteSpace(weapon.Description)
                ? CatalogPresentation.GetWeaponFallbackDescription()
                : weapon.Description,
            Badge = "Doporučujeme",
            BadgeClass = "badge-chip-restricted",
            ActionLabel = access.CanViewDetails ? "Zobrazit detail" : "Otevřít katalog zbraní",
            ActionController = "Weapons",
            ActionName = access.CanViewDetails ? "Details" : "Index",
            ActionRouteId = access.CanViewDetails ? weapon.Id : null,
            AccessRoute = access.CanViewDetails ? null : "restricted",
            AccessHint = access.CanViewDetails ? access.AccessLabel : access.RestrictionMessage
        };
    }

    private static string ResolveWeaponImagePath(Weapon weapon)
    {
        var uploadedImagePath = CatalogImageStorage.ToPublicPath(weapon.ImageFileName);
        if (!string.IsNullOrWhiteSpace(uploadedImagePath))
        {
            return uploadedImagePath;
        }

        var galleryImage = weapon.Images
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.Id)
            .Select(image => CatalogImageStorage.ToPublicPath(image.FileName))
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

        if (!string.IsNullOrWhiteSpace(galleryImage))
        {
            return galleryImage;
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

    private static string ResolveAccessoryImagePath(Accessory accessory)
    {
        var uploadedImagePath = CatalogImageStorage.ToPublicPath(accessory.ImageFileName);
        if (!string.IsNullOrWhiteSpace(uploadedImagePath))
        {
            return uploadedImagePath;
        }

        var galleryImage = accessory.Images
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.Id)
            .Select(image => CatalogImageStorage.ToPublicPath(image.FileName))
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

        if (!string.IsNullOrWhiteSpace(galleryImage))
        {
            return galleryImage;
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
