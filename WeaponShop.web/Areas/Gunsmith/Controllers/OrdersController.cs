using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeaponShop.Application.Interfaces;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Web.Helpers;
using WeaponShop.Web.ViewModels.Weapons;

namespace WeaponShop.Web.Areas.Gunsmith.Controllers;

[Area("Gunsmith")]
[Authorize(Roles = "Zbrojir")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IWeaponService _weaponService;
    private readonly IInvoiceDocumentService _invoiceDocumentService;

    public OrdersController(
        IOrderService orderService,
        IWeaponService weaponService,
        IInvoiceDocumentService invoiceDocumentService)
    {
        _orderService = orderService;
        _weaponService = weaponService;
        _invoiceDocumentService = invoiceDocumentService;
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
    public async Task<IActionResult> EditAssignedUnit(int id, int weaponId, int unitId, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var weapon = order.Items
            .Where(item => item.WeaponId == weaponId && item.Weapon is not null)
            .Select(item => item.Weapon!)
            .FirstOrDefault();
        if (weapon is null)
        {
            return NotFound();
        }

        var unit = weapon.Units.FirstOrDefault(candidate => candidate.Id == unitId);
        if (!IsUnitAssignedToOrder(unit, order.Id))
        {
            return NotFound();
        }

        return View(MapUnitInputModel(weapon, unit!));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> EditAssignedUnit(int id, int weaponId, int unitId, WeaponUnitInputModel model, CancellationToken cancellationToken)
    {
        if (weaponId != model.WeaponId || unitId != model.UnitId)
        {
            return BadRequest();
        }

        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var weapon = order.Items
            .Where(item => item.WeaponId == weaponId && item.Weapon is not null)
            .Select(item => item.Weapon!)
            .FirstOrDefault();
        if (weapon is null)
        {
            return NotFound();
        }

        var existingUnit = weapon.Units.FirstOrDefault(candidate => candidate.Id == unitId);
        if (!IsUnitAssignedToOrder(existingUnit, order.Id))
        {
            return NotFound();
        }

        PopulateUnitContext(model, weapon);
        ValidateUnitModel(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _weaponService.UpdateUnitAsync(weaponId, MapUnit(model, existingUnit!.Status, existingUnit.ReservedOrderId, existingUnit.SoldOrderId), cancellationToken);
        TempData["StatusMessage"] = "Sériová čísla a části zbraně byly aktualizovány.";
        return RedirectToAction(nameof(Details), new { id });
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
        actorRole = User.IsInRole("Zbrojir") ? "Zbrojíř" : "Neznámá role";
        return !string.IsNullOrWhiteSpace(userId);
    }

    private static bool IsUnitAssignedToOrder(WeaponUnit? unit, int orderId)
    {
        return unit is not null && (unit.ReservedOrderId == orderId || unit.SoldOrderId == orderId);
    }

    private void ValidateUnitModel(WeaponUnitInputModel model)
    {
        var seenSerialNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var serial in EnumerateUnitSerialNumbers(model))
        {
            if (!seenSerialNumbers.Add(serial))
            {
                ModelState.AddModelError(nameof(model.PrimarySerialNumber), $"Sériové číslo '{serial}' je v evidovaném kuse uvedeno vícekrát.");
            }
        }

        var optionalPartFilled = !string.IsNullOrWhiteSpace(model.Part4Name) || !string.IsNullOrWhiteSpace(model.Part4SerialNumber);
        if (optionalPartFilled && (string.IsNullOrWhiteSpace(model.Part4Name) || string.IsNullOrWhiteSpace(model.Part4SerialNumber)))
        {
            ModelState.AddModelError(nameof(model.Part4SerialNumber), "Volitelná čtvrtá část musí obsahovat název i sériové číslo.");
        }

    }

    private static IEnumerable<string> EnumerateUnitSerialNumbers(WeaponUnitInputModel model)
    {
        yield return model.PrimarySerialNumber.Trim();
        if (!string.IsNullOrWhiteSpace(model.Part1SerialNumber))
        {
            yield return model.Part1SerialNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(model.Part2SerialNumber))
        {
            yield return model.Part2SerialNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(model.Part3SerialNumber))
        {
            yield return model.Part3SerialNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(model.Part4SerialNumber))
        {
            yield return model.Part4SerialNumber.Trim();
        }
    }

    private static void PopulateUnitContext(WeaponUnitInputModel model, Weapon weapon)
    {
        model.WeaponName = weapon.Name;
        model.WeaponCategory = weapon.Category;
        model.WeaponTypeDesignation = weapon.TypeDesignation;
        model.IsCreate = false;
    }

    private static WeaponUnitInputModel MapUnitInputModel(Weapon weapon, WeaponUnit unit)
    {
        var parts = unit.Parts
            .OrderBy(part => part.SlotNumber)
            .ToDictionary(part => part.SlotNumber);

        return new WeaponUnitInputModel
        {
            WeaponId = weapon.Id,
            UnitId = unit.Id,
            WeaponName = weapon.Name,
            WeaponCategory = weapon.Category,
            WeaponTypeDesignation = weapon.TypeDesignation,
            IsCreate = false,
            PrimarySerialNumber = unit.PrimarySerialNumber,
            Part1Name = WeaponUnitInputModel.Part1FixedName,
            Part1SerialNumber = parts.GetValueOrDefault(1)?.SerialNumber ?? string.Empty,
            Part2Name = WeaponUnitInputModel.Part2FixedName,
            Part2SerialNumber = parts.GetValueOrDefault(2)?.SerialNumber ?? string.Empty,
            Part3Name = WeaponUnitInputModel.Part3FixedName,
            Part3SerialNumber = parts.GetValueOrDefault(3)?.SerialNumber ?? string.Empty,
            Part4Name = parts.GetValueOrDefault(4)?.PartName ?? string.Empty,
            Part4SerialNumber = parts.GetValueOrDefault(4)?.SerialNumber ?? string.Empty,
            Part4Notes = parts.GetValueOrDefault(4)?.Notes ?? string.Empty,
            UnitNotes = unit.Notes
        };
    }

    private static WeaponUnit MapUnit(WeaponUnitInputModel model, WeaponUnitStatus status, int? reservedOrderId, int? soldOrderId)
    {
        var parts = new List<WeaponUnitPart>();

        AddFixedPart(parts, 1, WeaponUnitInputModel.Part1FixedName, model.Part1SerialNumber);
        AddFixedPart(parts, 2, WeaponUnitInputModel.Part2FixedName, model.Part2SerialNumber);
        AddFixedPart(parts, 3, WeaponUnitInputModel.Part3FixedName, model.Part3SerialNumber);

        if (!string.IsNullOrWhiteSpace(model.Part4Name) && !string.IsNullOrWhiteSpace(model.Part4SerialNumber))
        {
            parts.Add(new WeaponUnitPart
            {
                SlotNumber = 4,
                PartName = model.Part4Name.Trim(),
                SerialNumber = model.Part4SerialNumber.Trim(),
                Notes = model.Part4Notes?.Trim() ?? string.Empty
            });
        }

        return new WeaponUnit
        {
            Id = model.UnitId,
            WeaponId = model.WeaponId,
            PrimarySerialNumber = model.PrimarySerialNumber.Trim(),
            Status = status,
            ReservedOrderId = reservedOrderId,
            SoldOrderId = soldOrderId,
            Notes = model.UnitNotes?.Trim() ?? string.Empty,
            Parts = parts
        };
    }

    private static void AddFixedPart(List<WeaponUnitPart> parts, int slotNumber, string partName, string? serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return;
        }

        parts.Add(new WeaponUnitPart
        {
            SlotNumber = slotNumber,
            PartName = partName.Trim(),
            SerialNumber = serialNumber.Trim()
        });
    }
}
