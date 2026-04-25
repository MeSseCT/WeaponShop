using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Domain;

public class AccessoryImage
{
    public int Id { get; set; }

    public int AccessoryId { get; set; }
    public Accessory? Accessory { get; set; }

    [Required]
    [StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
