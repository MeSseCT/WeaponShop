using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;
using WeaponShop.Application.Services;
using WeaponShop.Web.Helpers;

namespace WeaponShop.Web.Areas.Warehouse.Controllers;

[Area("Warehouse")]
[Authorize(Roles = "Skladnik")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IInvoiceDocumentService _invoiceDocumentService;

    public OrdersController(IOrderService orderService, IInvoiceDocumentService invoiceDocumentService)
    {
        _orderService = orderService;
        _invoiceDocumentService = invoiceDocumentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetWarehouseOrdersAsync(cancellationToken);
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
        return File(bytes, "application/rtf", $"order-{id}.rtf");
    }

    [HttpGet]
    public async Task<IActionResult> ViewInvoice(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var invoice = _invoiceDocumentService.BuildInvoice(order);
        return Content(invoice.HtmlContent, "text/html; charset=utf-8");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadInvoice(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var invoice = _invoiceDocumentService.BuildInvoice(order);
        return File(invoice.PdfContent, "application/pdf", invoice.PdfFileName);
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> AssignUnits(int id, int weaponId, int[] selectedUnitIds, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorName, out var actorRole))
        {
            return Challenge();
        }

        try
        {
            await _orderService.AssignWeaponUnitsAsync(id, weaponId, selectedUnitIds, actorUserId, actorName, actorRole, cancellationToken);
            TempData["StatusMessage"] = $"Objednávka č. {id} byla spárována s konkrétními výrobními čísly.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> SendToGunsmith(int id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorName, out var actorRole))
        {
            return Challenge();
        }

        try
        {
            await _orderService.MarkWarehouseCheckedAsync(id, actorUserId, actorName, actorRole, cancellationToken);
            TempData["StatusMessage"] = $"Objednávka č. {id} byla předána zbrojíři.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> MarkShipped(int id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorName, out var actorRole))
        {
            return Challenge();
        }

        try
        {
            await _orderService.MarkShippedAsync(id, actorUserId, actorName, actorRole, cancellationToken);
            TempData["StatusMessage"] = $"Objednávka č. {id} byla označena jako odeslaná.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> MarkReadyForPickup(int id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorName, out var actorRole))
        {
            return Challenge();
        }

        try
        {
            await _orderService.MarkReadyForPickupAsync(id, actorUserId, actorName, actorRole, cancellationToken);
            TempData["StatusMessage"] = $"Objednávka č. {id} je připravena k vyzvednutí.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> ConfirmPickup(int id, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorName, out var actorRole))
        {
            return Challenge();
        }

        try
        {
            await _orderService.MarkPickupHandedOverAsync(id, actorUserId, actorName, actorRole, cancellationToken);
            TempData["StatusMessage"] = $"Objednávka č. {id} byla předána zákazníkovi.";
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
        actorRole = User.IsInRole("Skladnik") ? "Skladník" : "Neznámá role";
        return !string.IsNullOrWhiteSpace(userId);
    }
}
