using WeaponShop.Domain;

namespace WeaponShop.Web.ViewModels.Weapons;

public class WeaponUnitRowViewModel
{
    public int UnitId { get; set; }
    public string PrimarySerialNumber { get; set; } = string.Empty;
    public WeaponUnitStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? ReservedOrderNumber { get; set; }
    public string? SoldOrderNumber { get; set; }
    public IReadOnlyList<string> PartSummaries { get; set; } = Array.Empty<string>();
}
