using WeaponShop.Domain.Identity;

namespace WeaponShop.Domain;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
