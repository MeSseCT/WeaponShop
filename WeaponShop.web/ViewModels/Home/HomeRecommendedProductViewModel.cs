namespace WeaponShop.Web.ViewModels.Home;

public class HomeRecommendedProductViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = "badge-chip-public";
    public string ActionLabel { get; set; } = string.Empty;
    public string ActionController { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public int? ActionRouteId { get; set; }
    public string? AccessRoute { get; set; }
    public string AccessHint { get; set; } = string.Empty;
}
