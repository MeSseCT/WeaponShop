using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeaponShop.Application.Interfaces;
using WeaponShop.Application.Services;
using WeaponShop.Domain;

namespace WeaponShop.Web.Controllers;

[Authorize(Roles = "Customer")]
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        try
        {
            var orders = await _orderService.GetSubmittedOrdersAsync(userId, cancellationToken);
            return View(orders);
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Objednávky se teď nepodařilo načíst.";
            return View(new List<Order>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        Order? order;
        try
        {
            order = await _orderService.GetByIdAsync(id, cancellationToken);
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Detail objednávky se teď nepodařilo načíst.";
            return RedirectToAction(nameof(Index));
        }

        if (order is null)
        {
            return NotFound();
        }

        if (!string.Equals(order.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> ViewInvoice(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (!string.Equals(order.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        var invoice = _invoiceDocumentService.BuildInvoice(order);
        return Content(invoice.HtmlContent, "text/html; charset=utf-8");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadInvoice(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (!string.Equals(order.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        var invoice = _invoiceDocumentService.BuildInvoice(order);
        return File(invoice.PdfContent, "application/pdf", invoice.PdfFileName);
    }
}
