using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WeaponShop.Web.ViewModels.Weapons;

public class WeaponInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(100)]
    [Display(Name = "Název")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Typ je povinný.")]
    [StringLength(120)]
    [Display(Name = "Typ / model")]
    public string TypeDesignation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategorie je povinná.")]
    [RegularExpression("^(B|C|CI|D)$", ErrorMessage = "Kategorie musí být B, C, C-I nebo D.")]
    [Display(Name = "Kategorie zbraně")]
    public string Category { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Popis")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Cena musí být mezi 0 a 100000.")]
    [Display(Name = "Cena")]
    public decimal Price { get; set; }

    [Display(Name = "Skladové množství")]
    public int StockQuantity { get; set; }

    [Display(Name = "Dostupné")]
    public bool IsAvailable { get; set; } = true;

    [Required(ErrorMessage = "Výrobce je povinný.")]
    [StringLength(100)]
    [Display(Name = "Výrobce")]
    public string Manufacturer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kalibr je povinný.")]
    [StringLength(50)]
    [Display(Name = "Kalibr")]
    public string Caliber { get; set; } = string.Empty;

    [Display(Name = "Hlavní obrázek")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Další obrázky")]
    public List<IFormFile> AdditionalImageFiles { get; set; } = new();

    [Display(Name = "Odstranit aktuální obrázek")]
    public bool RemoveImage { get; set; }
    public string? CurrentImagePath { get; set; }
    public bool HasImage { get; set; }
    public List<WeaponGalleryImageInputModel> ExistingGalleryImages { get; set; } = new();
    public List<WeaponRelatedOrderViewModel> RelatedOrders { get; set; } = new();
    public List<WeaponUnitRowViewModel> Units { get; set; } = new();
}
