using WeaponShop.Domain;

namespace WeaponShop.Web.ViewModels.Cart;

public class CartIndexViewModel
{
    public Order? CurrentOrder { get; set; }
    public IReadOnlyList<Order> SubmittedOrders { get; set; } = Array.Empty<Order>();
}
