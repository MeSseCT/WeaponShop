using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WeaponShop.Web.ViewModels.Accessories;

public class AccessoryInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(100)]
    [Display(Name = "Název")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategorie je povinná.")]
    [StringLength(100)]
    [Display(Name = "Kategorie")]
    public string Category { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Popis")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Cena musí být mezi 0 a 100000.")]
    [Display(Name = "Cena")]
    public decimal Price { get; set; }

    [Range(0, 500, ErrorMessage = "Skladové množství musí být mezi 0 a 500.")]
    [Display(Name = "Skladové množství")]
    public int StockQuantity { get; set; }

    [Display(Name = "Dostupné")]
    public bool IsAvailable { get; set; } = true;

    [Required(ErrorMessage = "Výrobce je povinný.")]
    [StringLength(100)]
    [Display(Name = "Výrobce")]
    public string Manufacturer { get; set; } = string.Empty;

    [Display(Name = "Hlavní obrázek")]
    public IFormFile? ImageFile { get; set; }

    [Display(Name = "Odstranit aktuální obrázek")]
    public bool RemoveImage { get; set; }
    public string? CurrentImagePath { get; set; }
    public bool HasImage { get; set; }
}
