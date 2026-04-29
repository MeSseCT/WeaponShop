using WeaponShop.Application.Services;
using WeaponShop.Domain;

namespace WeaponShop.Web.ViewModels.Cart;

public class CartIndexViewModel
{
    public Order? CurrentOrder { get; set; }
    public IReadOnlyList<Order> SubmittedOrders { get; set; } = Array.Empty<Order>();
    public CheckoutInputModel Checkout { get; set; } = new();

    public bool ContainsWeaponItems => CurrentOrder?.Items.Any(item => item.IsWeapon) == true;
    public bool RequiresManualApproval => CurrentOrder?.Items.Any(item => item.IsWeapon && WeaponCategoryPolicy.RequiresManualApproval(item.GetCategoryCode())) == true;
}
