using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Web.Helpers;

namespace WeaponShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAwaitingApprovalOrdersAsync(cancellationToken);
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> All(string? status, string? q, CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllOrdersAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
        {
            orders = orders.Where(order => order.Status == parsed).ToList();
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            orders = orders
                .Where(order =>
                    order.Id.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (order.User?.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || $"{order.User?.FirstName} {order.User?.LastName}".Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> History(string userId, string? status, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest();
        }

        var orders = await _orderService.GetUserHistoryAsync(userId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
        {
            orders = orders
                .Where(order => order.Status == parsed)
                .ToList();
        }
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadWord(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var bytes = OrderWordExport.BuildOrderDocument(order);
        return File(bytes, "application/msword", $"order-{id}.doc");
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> DeleteHistory(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            await _orderService.DeleteOrderAsync(id, cancellationToken);
            TempData["StatusMessage"] = $"Historie objednávky č. {id} byla smazána.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl)
            && Url.IsLocalUrl(returnUrl)
            && !returnUrl.Contains("/Details", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(All));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorName, out var actorRole))
        {
            return Challenge();
        }

        try
        {
            await _orderService.ApproveOrderAsync(id, actorUserId, actorName, actorRole, cancellationToken);
            TempData["StatusMessage"] = $"Objednávka č. {id} byla schválena a předána skladu.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorName, out var actorRole))
        {
            return Challenge();
        }

        try
        {
            await _orderService.RejectOrderAsync(id, actorUserId, actorName, actorRole, null, cancellationToken);
            TempData["StatusMessage"] = $"Objednávka č. {id} byla zamítnuta.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private bool TryGetActor(out string userId, out string actorName, out string actorRole)
    {
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        actorName = User.Identity?.Name ?? "neznámý uživatel";
        actorRole = User.IsInRole("Admin") ? "Admin" : "Unknown";
        return !string.IsNullOrWhiteSpace(userId);
    }
}
