using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Domain.Identity;
using WeaponShop.Web.ViewModels.Cart;

namespace WeaponShop.Web.Controllers;

[Authorize(Roles = "Customer")]
public class CartController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var user = await _userManager.GetUserAsync(User);
        var currentOrder = await _orderService.GetCurrentOrderAsync(userId, cancellationToken);

        var model = new CartIndexViewModel
        {
            CurrentOrder = currentOrder,
            SubmittedOrders = await _orderService.GetSubmittedOrdersAsync(userId, cancellationToken),
            Checkout = BuildCheckoutModel(currentOrder, user)
        };

        return View(model);
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Add(int? weaponId, int? accessoryId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            if (weaponId.HasValue)
            {
                await _orderService.AddItemToCurrentOrderAsync(userId, weaponId.Value, quantity, cancellationToken);
                TempData["StatusMessage"] = "Zbraň byla přidána do košíku.";
            }
            else if (accessoryId.HasValue)
            {
                await _orderService.AddAccessoryItemToCurrentOrderAsync(userId, accessoryId.Value, quantity, cancellationToken);
                TempData["StatusMessage"] = "Doplněk byl přidán do košíku.";
            }
            else
            {
                TempData["ErrorMessage"] = "Nebyla vybrána žádná položka k přidání do košíku.";
            }
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Remove(int? weaponId, int? accessoryId, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            if (weaponId.HasValue)
            {
                await _orderService.RemoveItemFromCurrentOrderAsync(userId, weaponId.Value, cancellationToken);
                TempData["StatusMessage"] = "Zbraň byla odebrána z košíku.";
            }
            else if (accessoryId.HasValue)
            {
                await _orderService.RemoveAccessoryItemFromCurrentOrderAsync(userId, accessoryId.Value, cancellationToken);
                TempData["StatusMessage"] = "Doplněk byl odebrán z košíku.";
            }
            else
            {
                TempData["ErrorMessage"] = "Nebyla vybrána žádná položka k odebrání.";
            }
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Checkout([Bind(Prefix = "Checkout")] CheckoutInputModel model, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var user = await _userManager.GetUserAsync(User);
        var currentOrder = await _orderService.GetCurrentOrderAsync(userId, cancellationToken);
        if (currentOrder is null)
        {
            TempData["ErrorMessage"] = "Aktuální objednávka nebyla nalezena.";
            return RedirectToAction(nameof(Index));
        }

        if (model.BillingSameAsShipping)
        {
            model.BillingName = model.ShippingName;
            model.BillingStreet = model.ShippingStreet;
            model.BillingCity = model.ShippingCity;
            model.BillingPostalCode = model.ShippingPostalCode;

            ModelState.Remove("Checkout.BillingName");
            ModelState.Remove("Checkout.BillingStreet");
            ModelState.Remove("Checkout.BillingCity");
            ModelState.Remove("Checkout.BillingPostalCode");
        }

        if (currentOrder.Items.Any(item => item.IsWeapon) && !string.Equals(model.DeliveryMethod, "pickup", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.DeliveryMethod), "Objednávky se zbraněmi lze dokončit pouze s osobním odběrem.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = new CartIndexViewModel
            {
                CurrentOrder = currentOrder,
                SubmittedOrders = await _orderService.GetSubmittedOrdersAsync(userId, cancellationToken),
                Checkout = model
            };
            return View(nameof(Index), invalidModel);
        }

        try
        {
            await _orderService.CheckoutCurrentOrderAsync(userId, new CheckoutDetails
            {
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone,
                DeliveryMethod = model.DeliveryMethod,
                PaymentMethod = model.PaymentMethod,
                ShippingName = model.ShippingName,
                ShippingStreet = model.ShippingStreet,
                ShippingCity = model.ShippingCity,
                ShippingPostalCode = model.ShippingPostalCode,
                BillingName = model.BillingName,
                BillingStreet = model.BillingStreet,
                BillingCity = model.BillingCity,
                BillingPostalCode = model.BillingPostalCode,
                CustomerNote = model.CustomerNote
            }, cancellationToken);

            TempData["StatusMessage"] = "Objednávka byla úspěšně odeslána.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);

            var invalidModel = new CartIndexViewModel
            {
                CurrentOrder = currentOrder,
                SubmittedOrders = await _orderService.GetSubmittedOrdersAsync(userId, cancellationToken),
                Checkout = model
            };
            return View(nameof(Index), invalidModel);
        }
    }

    private static CheckoutInputModel BuildCheckoutModel(Order? currentOrder, ApplicationUser? user)
    {
        if (currentOrder is not null && !string.IsNullOrWhiteSpace(currentOrder.ContactEmail))
        {
            return new CheckoutInputModel
            {
                ContactEmail = currentOrder.ContactEmail,
                ContactPhone = currentOrder.ContactPhone,
                DeliveryMethod = string.IsNullOrWhiteSpace(currentOrder.DeliveryMethod) ? "pickup" : currentOrder.DeliveryMethod,
                PaymentMethod = string.IsNullOrWhiteSpace(currentOrder.PaymentMethod) ? "bank-transfer" : currentOrder.PaymentMethod,
                ShippingName = currentOrder.ShippingName,
                ShippingStreet = currentOrder.ShippingStreet,
                ShippingCity = currentOrder.ShippingCity,
                ShippingPostalCode = currentOrder.ShippingPostalCode,
                BillingName = currentOrder.BillingName,
                BillingStreet = currentOrder.BillingStreet,
                BillingCity = currentOrder.BillingCity,
                BillingPostalCode = currentOrder.BillingPostalCode,
                CustomerNote = currentOrder.CustomerNote,
                BillingSameAsShipping = string.Equals(currentOrder.ShippingName, currentOrder.BillingName, StringComparison.Ordinal)
                    && string.Equals(currentOrder.ShippingStreet, currentOrder.BillingStreet, StringComparison.Ordinal)
                    && string.Equals(currentOrder.ShippingCity, currentOrder.BillingCity, StringComparison.Ordinal)
                    && string.Equals(currentOrder.ShippingPostalCode, currentOrder.BillingPostalCode, StringComparison.Ordinal)
            };
        }

        var fullName = $"{user?.FirstName} {user?.LastName}".Trim();
        return new CheckoutInputModel
        {
            ContactEmail = user?.Email ?? string.Empty,
            ContactPhone = user?.PhoneNumber ?? string.Empty,
            DeliveryMethod = "pickup",
            PaymentMethod = "bank-transfer",
            ShippingName = fullName,
            BillingName = fullName
        };
    }
}
