using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Web.Helpers;

namespace WeaponShop.Web.Areas.Gunsmith.Controllers;

[Area("Gunsmith")]
[Authorize(Roles = "Zbrojir")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetGunsmithOrdersAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var statusFilter))
        {
            orders = orders.Where(order => order.Status == statusFilter).ToList();
        }
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out _, out _))
        {
            return Challenge();
        }

        var audits = await _orderService.GetAuditsByActorAsync(actorUserId, cancellationToken);
        return View(audits);
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
    public async Task<IActionResult> MarkChecked(int id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorName, out var actorRole))
        {
            return Challenge();
        }

        try
        {
            await _orderService.MarkGunsmithCheckedAsync(id, actorUserId, actorName, actorRole, cancellationToken);
            TempData["StatusMessage"] = $"Objednávka č. {id} byla zkontrolována a vrácena na sklad.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Reject(int id, string? reason, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorName, out var actorRole))
        {
            return Challenge();
        }

        try
        {
            await _orderService.RejectOrderAsync(id, actorUserId, actorName, actorRole, reason, cancellationToken);
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
        actorRole = User.IsInRole("Zbrojir") ? "Zbrojir" : "Unknown";
        return !string.IsNullOrWhiteSpace(userId);
    }
}
