using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Domain;

public class WeaponImage
{
    public int Id { get; set; }

    public int WeaponId { get; set; }
    public Weapon? Weapon { get; set; }

    [Required]
    [StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
