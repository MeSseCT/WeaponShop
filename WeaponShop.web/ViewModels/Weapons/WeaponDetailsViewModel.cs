using WeaponShop.Domain;

namespace WeaponShop.Web.ViewModels.Weapons;

public class WeaponDetailsViewModel
{
    public Weapon Weapon { get; set; } = new();
    public string ImagePath { get; set; } = string.Empty;
    public bool CanAddToCart { get; set; }
    public bool CanViewRestrictedCatalog { get; set; }
    public IReadOnlyList<StoreCatalogItemViewModel> RelatedProducts { get; set; } = Array.Empty<StoreCatalogItemViewModel>();
}
