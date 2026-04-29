using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Web.ViewModels.Weapons;

public class WeaponUnitInputModel
{
    public const string Part1FixedName = "Hlaveň";
    public const string Part2FixedName = "Závěr";
    public const string Part3FixedName = "Rám";

    public int WeaponId { get; set; }
    public int UnitId { get; set; }
    public string WeaponName { get; set; } = string.Empty;
    public string WeaponCategory { get; set; } = string.Empty;
    public string WeaponTypeDesignation { get; set; } = string.Empty;
    public bool IsCreate { get; set; }

    [Required(ErrorMessage = "Hlavní výrobní číslo je povinné.")]
    [StringLength(120)]
    [Display(Name = "Hlavní výrobní číslo")]
    public string PrimarySerialNumber { get; set; } = string.Empty;

    public string Part1Name { get; set; } = Part1FixedName;

    [Required(ErrorMessage = "Sériové číslo hlavně je povinné.")]
    [StringLength(120)]
    [Display(Name = "Sériové číslo hlavně")]
    public string? Part1SerialNumber { get; set; }

    public string Part2Name { get; set; } = Part2FixedName;

    [Required(ErrorMessage = "Sériové číslo závěru je povinné.")]
    [StringLength(120)]
    [Display(Name = "Sériové číslo závěru")]
    public string? Part2SerialNumber { get; set; }

    public string Part3Name { get; set; } = Part3FixedName;

    [Required(ErrorMessage = "Sériové číslo rámu je povinné.")]
    [StringLength(120)]
    [Display(Name = "Sériové číslo rámu")]
    public string? Part3SerialNumber { get; set; }

    [StringLength(120)]
    [Display(Name = "Část 4 (volitelné)")]
    public string? Part4Name { get; set; }

    [StringLength(120)]
    [Display(Name = "Sériové číslo 4")]
    public string? Part4SerialNumber { get; set; }

    [StringLength(300)]
    [Display(Name = "Poznámka k části 4")]
    public string? Part4Notes { get; set; }

    [StringLength(300)]
    [Display(Name = "Interní poznámka")]
    public string? UnitNotes { get; set; }
}
