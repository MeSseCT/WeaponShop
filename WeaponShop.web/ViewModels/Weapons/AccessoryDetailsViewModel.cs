using WeaponShop.Domain;

namespace WeaponShop.Web.ViewModels.Weapons;

public class AccessoryDetailsViewModel
{
    public Accessory Accessory { get; set; } = new();
    public string ImagePath { get; set; } = string.Empty;
    public IReadOnlyList<StoreCatalogItemViewModel> RelatedProducts { get; set; } = Array.Empty<StoreCatalogItemViewModel>();
}
