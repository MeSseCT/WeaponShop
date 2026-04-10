namespace WeaponShop.Web.ViewModels.Weapons;

public class StoreCatalogItemViewModel
{
    public int? WeaponId { get; set; }
    public int? AccessoryId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsRestricted { get; set; }
    public string AccessLabel { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string AccentClass { get; set; } = "accent-default";
    public bool HasDetailPage => WeaponId.HasValue || AccessoryId.HasValue;
}
