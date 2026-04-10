using WeaponShop.Domain;

namespace WeaponShop.Web.ViewModels.Cart;

public class CartIndexViewModel
{
    public Order? CurrentOrder { get; set; }
    public IReadOnlyList<Order> SubmittedOrders { get; set; } = Array.Empty<Order>();
    public CheckoutInputModel Checkout { get; set; } = new();

    public bool ContainsRestrictedItems => CurrentOrder?.Items.Any(item => item.IsWeapon) == true;
}
