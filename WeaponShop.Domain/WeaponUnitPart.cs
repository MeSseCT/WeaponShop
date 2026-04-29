using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Domain;

public class WeaponUnitPart
{
    public int Id { get; set; }

    public int WeaponUnitId { get; set; }
    public WeaponUnit? WeaponUnit { get; set; }

    public int SlotNumber { get; set; }

    [Required]
    [StringLength(120)]
    public string PartName { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string SerialNumber { get; set; } = string.Empty;

    [StringLength(300)]
    public string Notes { get; set; } = string.Empty;
}
