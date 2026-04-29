using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using WeaponShop.Domain;
using WeaponShop.Domain.Identity;
using WeaponShop.Web.Controllers;
using WeaponShop.Web.ViewModels.Cart;

namespace WeaponShop.Tests.Controllers;

public class CartControllerTests
{
    [Fact]
    public async Task Add_Accessory_RedirectsAndCallsOrderService()
    {
        var user = new ApplicationUser
        {
            Id = "customer-1",
            UserName = "customer@weaponshop.local",
            Email = "customer@weaponshop.local"
        };

        var orderService = new TestOrderService();
        var controller = CreateController(orderService, new TestUserManager(new[] { user }), user);

        var result = await controller.Add(weaponId: null, accessoryId: 5, quantity: 2, cancellationToken: CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(5, orderService.AddedAccessoryId);
        Assert.Equal(2, orderService.AddedQuantity);
        Assert.Equal(user.Id, orderService.LastUserId);
    }

    [Fact]
    public async Task Checkout_WithWeaponAndDeliveryOtherThanPickup_ReturnsValidationError()
    {
        var user = new ApplicationUser
        {
            Id = "customer-2",
            UserName = "customer2@weaponshop.local",
            Email = "customer2@weaponshop.local"
        };

        var orderService = new TestOrderService
        {
            CurrentOrder = new Order
            {
                Id = 10,
                UserId = user.Id,
                Items =
                [
                    new OrderItem
                    {
                        WeaponId = 7,
                        Quantity = 1,
                        UnitPrice = 1000m
                    }
                ]
            }
        };

        var controller = CreateController(orderService, new TestUserManager(new[] { user }), user);
        var model = new CheckoutInputModel
        {
            ContactEmail = user.Email!,
            ContactPhone = "123456789",
            DeliveryMethod = "delivery",
            PaymentMethod = "bank-transfer",
            ShippingName = "Customer User",
            ShippingStreet = "Main street 1",
            ShippingCity = "Bratislava",
            ShippingPostalCode = "81101",
            BillingSameAsShipping = true
        };

        var result = await controller.Checkout(model, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.False(orderService.CheckoutCalled);
        Assert.Contains(controller.ModelState[nameof(model.DeliveryMethod)]!.Errors, error =>
            error.ErrorMessage.Contains("osobním odběrem", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Remove_Accessory_RedirectsAndCallsOrderService()
    {
        var user = new ApplicationUser
        {
            Id = "customer-3",
            UserName = "customer3@weaponshop.local",
            Email = "customer3@weaponshop.local"
        };

        var orderService = new TestOrderService();
        var controller = CreateController(orderService, new TestUserManager(new[] { user }), user);

        var result = await controller.Remove(weaponId: null, accessoryId: 9, cancellationToken: CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(9, orderService.RemovedAccessoryId);
        Assert.Equal(user.Id, orderService.LastUserId);
    }

    [Fact]
    public async Task Checkout_BillingSameAsShipping_CopiesAddressAndCallsCheckout()
    {
        var user = new ApplicationUser
        {
            Id = "customer-4",
            UserName = "customer4@weaponshop.local",
            Email = "customer4@weaponshop.local",
            FirstName = "Cart",
            LastName = "User"
        };

        var orderService = new TestOrderService
        {
            CurrentOrder = new Order
            {
                Id = 20,
                UserId = user.Id,
                Items =
                [
                    new OrderItem
                    {
                        AccessoryId = 3,
                        Quantity = 1,
                        UnitPrice = 25m
                    }
                ]
            }
        };

        var controller = CreateController(orderService, new TestUserManager(new[] { user }), user);
        var model = new CheckoutInputModel
        {
            ContactEmail = user.Email!,
            ContactPhone = "123456789",
            DeliveryMethod = "delivery",
            PaymentMethod = "bank-transfer",
            ShippingName = "Cart User",
            ShippingStreet = "Main street 1",
            ShippingCity = "Bratislava",
            ShippingPostalCode = "81101",
            BillingSameAsShipping = true
        };

        var result = await controller.Checkout(model, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.True(orderService.CheckoutCalled);
        Assert.NotNull(orderService.LastCheckoutDetails);
        Assert.Equal("Cart User", orderService.LastCheckoutDetails!.BillingName);
        Assert.Equal("Main street 1", orderService.LastCheckoutDetails.BillingStreet);
        Assert.Equal("Bratislava", orderService.LastCheckoutDetails.BillingCity);
        Assert.Equal("81101", orderService.LastCheckoutDetails.BillingPostalCode);
    }

    private static CartController CreateController(
        TestOrderService orderService,
        TestUserManager userManager,
        ApplicationUser user)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
                new Claim(ClaimTypes.Role, "Customer")
            ], "Test"))
        };

        return new CartController(orderService, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }
}
