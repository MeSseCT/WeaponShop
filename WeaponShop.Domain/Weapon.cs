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

    /// <summary>
    /// Weapon category (A/B/C/D/E based on local legislation).
    /// </summary>
    [Required]
    [RegularExpression("^[ABCDE]$", ErrorMessage = "Category must be one of: A, B, C, D, E.")]
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

    [StringLength(260)]
    public string? ImageFileName { get; set; }
}
