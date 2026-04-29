using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Domain;

public class WeaponUnit
{
    public int Id { get; set; }

    public int WeaponId { get; set; }
    public Weapon? Weapon { get; set; }

    [Required]
    [StringLength(120)]
    public string PrimarySerialNumber { get; set; } = string.Empty;

    public WeaponUnitStatus Status { get; set; } = WeaponUnitStatus.InStock;

    public int? ReservedOrderId { get; set; }
    public Order? ReservedOrder { get; set; }

    public int? SoldOrderId { get; set; }
    public Order? SoldOrder { get; set; }

    [StringLength(300)]
    public string Notes { get; set; } = string.Empty;

    public ICollection<WeaponUnitPart> Parts { get; set; } = new List<WeaponUnitPart>();
}
