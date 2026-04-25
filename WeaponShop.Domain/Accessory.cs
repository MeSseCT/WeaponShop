using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Domain;

public class Accessory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [Range(0, 500)]
    public int StockQuantity { get; set; }

    public bool IsAvailable { get; set; } = true;

    [Required]
    [StringLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [StringLength(260)]
    public string? ImageFileName { get; set; }

    public List<AccessoryImage> Images { get; set; } = new();
}
