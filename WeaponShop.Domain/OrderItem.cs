namespace WeaponShop.Domain;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int WeaponId { get; set; }
    public Weapon? Weapon { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
