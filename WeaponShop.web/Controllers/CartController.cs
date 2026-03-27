using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeaponShop.Application.Services;

namespace WeaponShop.Web.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly IOrderService _orderService;

    public CartController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var order = await _orderService.GetCurrentOrderAsync(userId, cancellationToken);
        return View(order);
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Add(int weaponId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            await _orderService.AddItemToCurrentOrderAsync(userId, weaponId, quantity, cancellationToken);
            TempData["StatusMessage"] = "Item was added to cart.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Remove(int weaponId, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            await _orderService.RemoveItemFromCurrentOrderAsync(userId, weaponId, cancellationToken);
            TempData["StatusMessage"] = "Item was removed from cart.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            await _orderService.CheckoutCurrentOrderAsync(userId, cancellationToken);
            TempData["StatusMessage"] = "Order was submitted and is awaiting approval.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
