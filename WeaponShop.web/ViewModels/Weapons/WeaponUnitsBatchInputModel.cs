using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Web.ViewModels.Weapons;

public class WeaponUnitsBatchInputModel
{
    public int WeaponId { get; set; }
    public string WeaponName { get; set; } = string.Empty;
    public string WeaponCategory { get; set; } = string.Empty;
    public string WeaponTypeDesignation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zadejte alespoň jedno hlavní sériové číslo.")]
    [Display(Name = "Hlavní sériová čísla")]
    public string SerialNumbersText { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Společná poznámka")]
    public string? CommonNotes { get; set; }
}
