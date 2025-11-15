using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Domain;

/// <summary>
/// Represents a weapon in the shop catalogue.
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

    [Required]
    [StringLength(100)]
    public string Manufacturer { get; set; } = string.Empty;
}

