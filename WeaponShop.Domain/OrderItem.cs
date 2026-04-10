namespace WeaponShop.Domain;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int? WeaponId { get; set; }
    public Weapon? Weapon { get; set; }
    public int? AccessoryId { get; set; }
    public Accessory? Accessory { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public bool IsWeapon => WeaponId.HasValue;
    public bool IsAccessory => AccessoryId.HasValue;

    public string GetDisplayName()
    {
        return Weapon?.Name
            ?? Accessory?.Name
            ?? "Neznámá položka";
    }

    public string GetCategoryCode()
    {
        return Weapon?.Category
            ?? Accessory?.Category
            ?? string.Empty;
    }
}
