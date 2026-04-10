namespace WeaponShop.Web.ViewModels.Weapons;

public class StoreCatalogViewModel
{
    public string Search { get; set; } = string.Empty;
    public string AccessFilter { get; set; } = "all";
    public string AccessoryCategoryFilter { get; set; } = string.Empty;
    public string LegalCategoryFilter { get; set; } = string.Empty;
    public string ManufacturerFilter { get; set; } = string.Empty;
    public string SortBy { get; set; } = "featured";
    public bool IsAuthenticated { get; set; }
    public bool IsAdultConfirmed { get; set; }
    public bool HasVerificationDocuments { get; set; }
    public bool CanViewRestrictedCatalog { get; set; }
    public IReadOnlyList<string> Manufacturers { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AccessoryCategories { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> LegalCategories { get; set; } = Array.Empty<string>();
    public IReadOnlyList<StoreCatalogItemViewModel> AccessoryProducts { get; set; } = Array.Empty<StoreCatalogItemViewModel>();
    public IReadOnlyList<StoreCatalogItemViewModel> RestrictedProducts { get; set; } = Array.Empty<StoreCatalogItemViewModel>();
    public IReadOnlyList<StoreCatalogItemViewModel> RestrictedPreviewProducts { get; set; } = Array.Empty<StoreCatalogItemViewModel>();
}
