using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Web.ViewModels.Weapons;

public class WeaponInputModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[ABCDE]$")]
    public string Category { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [Range(0, 10)]
    public int StockQuantity { get; set; }

    public bool IsAvailable { get; set; } = true;

    [Required]
    [StringLength(100)]
    public string Manufacturer { get; set; } = string.Empty;
}
