using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeaponShop.Application.Interfaces;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Web.Helpers;

namespace WeaponShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
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
                .Where(order => MatchesOrderSearch(order, term))
                .ToList();
        }

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> History(string userId, string? status, string? q, CancellationToken cancellationToken)
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

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            orders = orders
                .Where(order => MatchesOrderSearch(order, term))
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
        actorRole = User.IsInRole("Admin") ? "Administrátor" : "Neznámá role";
        return !string.IsNullOrWhiteSpace(userId);
    }

    private static bool MatchesOrderSearch(Order order, string term)
    {
        return Contains(order.GetPublicOrderNumber(), term)
            || Contains(order.User?.Email, term)
            || Contains($"{order.User?.FirstName} {order.User?.LastName}".Trim(), term)
            || order.Items.Any(item =>
                Contains(item.GetDisplayName(), term)
                || (item.Weapon is not null && (
                    Contains(item.Weapon.TypeDesignation, term)
                    || item.Weapon.Units.Any(unit =>
                        Contains(unit.PrimarySerialNumber, term)
                        || unit.Parts.Any(part =>
                            Contains(part.SerialNumber, term)
                            || Contains(part.PartName, term)))))
                || (item.Accessory is not null && Contains(item.Accessory.Name, term)));
    }

    private static bool Contains(string? value, string term)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
