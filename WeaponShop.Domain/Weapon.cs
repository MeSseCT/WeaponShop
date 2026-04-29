using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Domain;

/// <summary>
/// </summary>
public class Weapon
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string TypeDesignation { get; set; } = string.Empty;

    /// <summary>
    /// Weapon category (B/C/CI/D based on local legislation used in the thesis).
    /// </summary>
    [Required]
    [RegularExpression("^(B|C|CI|D)$", ErrorMessage = "Kategorie musí být jedna z hodnot B, C, C-I nebo D.")]
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

    [Required]
    [StringLength(50)]
    public string Caliber { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string PrimarySerialNumber { get; set; } = string.Empty;

    [StringLength(260)]
    public string? ImageFileName { get; set; }

    public List<WeaponImage> Images { get; set; } = new();
    public List<WeaponUnit> Units { get; set; } = new();
}
