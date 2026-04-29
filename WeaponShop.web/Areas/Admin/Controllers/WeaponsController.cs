using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Web.Helpers;
using WeaponShop.Web.ViewModels.Weapons;

namespace WeaponShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Skladnik")]
public class WeaponsController : Controller
{
    private readonly IWeaponService _weaponService;
    private readonly IOrderService _orderService;
    private readonly IWebHostEnvironment _environment;

    public WeaponsController(IWeaponService weaponService, IOrderService orderService, IWebHostEnvironment environment)
    {
        _weaponService = weaponService;
        _orderService = orderService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        var weapons = await _weaponService.GetAllAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            weapons = weapons
                .Where(weapon => MatchesSearch(weapon, term))
                .ToList();
        }

        ViewData["Search"] = search?.Trim() ?? string.Empty;
        return View(weapons);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ViewData["FormMode"] = "Create";
        return View(new WeaponInputModel
        {
            IsAvailable = true
        });
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(WeaponInputModel model, CancellationToken cancellationToken)
    {
        ViewData["FormMode"] = "Create";
        ValidateImageInputs(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var weapon = MapToEntity(model);

        if (model.ImageFile is not null)
        {
            weapon.ImageFileName = await CatalogImageStorage.SaveAsync(_environment, model.ImageFile, "weapon", cancellationToken);
        }

        if (model.AdditionalImageFiles.Count > 0)
        {
            var sortOrder = 0;
            foreach (var additionalImage in model.AdditionalImageFiles.Where(file => file is not null && file.Length > 0))
            {
                weapon.Images.Add(new WeaponImage
                {
                    FileName = await CatalogImageStorage.SaveAsync(_environment, additionalImage, "weapon", cancellationToken),
                    SortOrder = sortOrder++
                });
            }
        }

        await _weaponService.AddAsync(weapon, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(id, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        ViewData["FormMode"] = "Edit";
        var model = await BuildWeaponInputModelAsync(weapon, cancellationToken);
        return View(model);
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, WeaponInputModel model, CancellationToken cancellationToken)
    {
        ViewData["FormMode"] = "Edit";
        if (id != model.Id)
        {
            return BadRequest();
        }

        var existing = await _weaponService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        ValidateImageInputs(model);

        if (!ModelState.IsValid)
        {
            ApplyImageState(model, existing);
            model.RelatedOrders = await LoadRelatedOrdersAsync(id, cancellationToken);
            model.Units = BuildUnitRows(existing);
            return View(model);
        }

        existing.Name = model.Name;
        existing.TypeDesignation = model.TypeDesignation;
        existing.Category = model.Category;
        existing.Description = model.Description;
        existing.Price = model.Price;
        existing.Manufacturer = model.Manufacturer;
        existing.Caliber = model.Caliber;
        existing.IsAvailable = model.IsAvailable;

        if (!IsSkladnikOnly())
        {
            if (model.RemoveImage)
            {
                CatalogImageStorage.DeleteIfExists(_environment, existing.ImageFileName);
                existing.ImageFileName = null;
            }

            if (model.ImageFile is not null)
            {
                CatalogImageStorage.DeleteIfExists(_environment, existing.ImageFileName);
                existing.ImageFileName = await CatalogImageStorage.SaveAsync(_environment, model.ImageFile, "weapon", cancellationToken);
            }

            var galleryImagesToRemove = model.ExistingGalleryImages
                .Where(image => image.Remove)
                .Select(image => image.Id)
                .ToHashSet();

            if (galleryImagesToRemove.Count > 0)
            {
                var removedImages = existing.Images
                    .Where(image => galleryImagesToRemove.Contains(image.Id))
                    .ToList();

                foreach (var removedImage in removedImages)
                {
                    CatalogImageStorage.DeleteIfExists(_environment, removedImage.FileName);
                    existing.Images.Remove(removedImage);
                }
            }

            var nextSortOrder = existing.Images.Count == 0
                ? 0
                : existing.Images.Max(image => image.SortOrder) + 1;

            foreach (var additionalImage in model.AdditionalImageFiles.Where(file => file is not null && file.Length > 0))
            {
                existing.Images.Add(new WeaponImage
                {
                    FileName = await CatalogImageStorage.SaveAsync(_environment, additionalImage, "weapon", cancellationToken),
                    SortOrder = nextSortOrder++
                });
            }
        }

        await _weaponService.UpdateAsync(existing, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Skladnik")]
    public async Task<IActionResult> CreateUnitsBatch(int weaponId, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(weaponId, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        return View(new WeaponUnitsBatchInputModel
        {
            WeaponId = weaponId,
            WeaponName = weapon.Name,
            WeaponCategory = weapon.Category,
            WeaponTypeDesignation = weapon.TypeDesignation
        });
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    [Authorize(Roles = "Admin,Skladnik")]
    public async Task<IActionResult> CreateUnitsBatch(WeaponUnitsBatchInputModel model, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(model.WeaponId, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        PopulateUnitsBatchContext(model, weapon);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var serialNumbers = model.SerialNumbersText
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (serialNumbers.Count == 0)
        {
            ModelState.AddModelError(nameof(model.SerialNumbersText), "Zadejte alespoň jedno hlavní sériové číslo.");
            return View(model);
        }

        foreach (var serialNumber in serialNumbers)
        {
            await _weaponService.AddUnitAsync(model.WeaponId, new WeaponUnit
            {
                PrimarySerialNumber = serialNumber,
                Notes = model.CommonNotes?.Trim() ?? string.Empty
            }, cancellationToken);
        }

        TempData["StatusMessage"] = $"Bylo naskladněno {serialNumbers.Count} kusů zbraně.";
        return RedirectToAction(nameof(Edit), new { id = model.WeaponId });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Skladnik,Zbrojir")]
    public async Task<IActionResult> EditUnit(int weaponId, int unitId, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(weaponId, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        var unit = await _weaponService.GetUnitByIdAsync(weaponId, unitId, cancellationToken);
        if (unit is null)
        {
            return NotFound();
        }

        if (unit.Status is WeaponUnitStatus.Reserved or WeaponUnitStatus.Sold)
        {
            TempData["ErrorMessage"] = "Rezervovaný nebo prodaný kus nelze upravovat.";
            return RedirectToAction(nameof(Edit), new { id = weaponId });
        }

        return View(MapUnitInputModel(weapon, unit));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    [Authorize(Roles = "Admin,Skladnik,Zbrojir")]
    public async Task<IActionResult> EditUnit(int weaponId, int unitId, WeaponUnitInputModel model, CancellationToken cancellationToken)
    {
        if (weaponId != model.WeaponId || unitId != model.UnitId)
        {
            return BadRequest();
        }

        var weapon = await _weaponService.GetByIdAsync(weaponId, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        PopulateUnitContext(model, weapon, isCreate: false);
        ValidateUnitModel(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _weaponService.UpdateUnitAsync(weaponId, MapUnit(model), cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = weaponId });
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    [Authorize(Roles = "Admin,Skladnik")]
    public async Task<IActionResult> DeleteUnit(int weaponId, int unitId, CancellationToken cancellationToken)
    {
        try
        {
            await _weaponService.DeleteUnitAsync(weaponId, unitId, cancellationToken);
            TempData["StatusMessage"] = "Evidovaný kus byl odstraněn.";
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = weaponId });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(id, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        return View(weapon);
    }

    [ValidateAntiForgeryToken]
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var existing = await _weaponService.GetByIdAsync(id, cancellationToken);
        if (existing is not null)
        {
            CatalogImageStorage.DeleteIfExists(_environment, existing.ImageFileName);
            foreach (var galleryImage in existing.Images)
            {
                CatalogImageStorage.DeleteIfExists(_environment, galleryImage.FileName);
            }
        }

        await _weaponService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private bool IsSkladnikOnly()
    {
        return User.IsInRole("Skladnik") && !User.IsInRole("Admin");
    }

    private async Task<WeaponInputModel> BuildWeaponInputModelAsync(Weapon weapon, CancellationToken cancellationToken)
    {
        var model = MapToInputModel(weapon);
        model.RelatedOrders = await LoadRelatedOrdersAsync(weapon.Id, cancellationToken);
        model.Units = BuildUnitRows(weapon);
        return model;
    }

    private static WeaponInputModel MapToInputModel(Weapon weapon)
    {
        return new WeaponInputModel
        {
            Id = weapon.Id,
            Name = weapon.Name,
            TypeDesignation = weapon.TypeDesignation,
            Category = weapon.Category,
            Description = weapon.Description,
            Price = weapon.Price,
            Manufacturer = weapon.Manufacturer,
            Caliber = weapon.Caliber,
            StockQuantity = weapon.StockQuantity,
            IsAvailable = weapon.IsAvailable,
            CurrentImagePath = CatalogImageStorage.ToPublicPath(weapon.ImageFileName),
            HasImage = !string.IsNullOrWhiteSpace(weapon.ImageFileName),
            ExistingGalleryImages = weapon.Images
                .OrderBy(image => image.SortOrder)
                .ThenBy(image => image.Id)
                .Select(image => new WeaponGalleryImageInputModel
                {
                    Id = image.Id,
                    ImagePath = CatalogImageStorage.ToPublicPath(image.FileName) ?? string.Empty
                })
                .ToList()
        };
    }

    private static Weapon MapToEntity(WeaponInputModel model)
    {
        return new Weapon
        {
            Id = model.Id,
            Name = model.Name,
            TypeDesignation = model.TypeDesignation,
            Category = model.Category,
            Description = model.Description,
            Price = model.Price,
            Manufacturer = model.Manufacturer,
            Caliber = model.Caliber,
            StockQuantity = 0,
            IsAvailable = model.IsAvailable
        };
    }

    private static void ApplyImageState(WeaponInputModel model, Weapon weapon)
    {
        model.CurrentImagePath = CatalogImageStorage.ToPublicPath(weapon.ImageFileName);
        model.HasImage = !string.IsNullOrWhiteSpace(weapon.ImageFileName);
        model.StockQuantity = weapon.StockQuantity;
        model.Units = BuildUnitRows(weapon);
        model.ExistingGalleryImages = weapon.Images
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.Id)
            .Select(image => new WeaponGalleryImageInputModel
            {
                Id = image.Id,
                ImagePath = CatalogImageStorage.ToPublicPath(image.FileName) ?? string.Empty,
                Remove = model.ExistingGalleryImages.FirstOrDefault(item => item.Id == image.Id)?.Remove ?? false
            })
            .ToList();
    }

    private void ValidateImageInputs(WeaponInputModel model)
    {
        if (!CatalogImageStorage.IsValidImage(model.ImageFile, out var validationError))
        {
            ModelState.AddModelError(nameof(model.ImageFile), validationError!);
        }

        foreach (var additionalImage in model.AdditionalImageFiles.Where(file => file is not null && file.Length > 0))
        {
            if (!CatalogImageStorage.IsValidImage(additionalImage, out validationError))
            {
                ModelState.AddModelError(nameof(model.AdditionalImageFiles), validationError!);
                break;
            }
        }
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

    private static bool MatchesSearch(Weapon weapon, string term)
    {
        return Contains(weapon.Name, term)
            || Contains(weapon.TypeDesignation, term)
            || Contains(weapon.Manufacturer, term)
            || Contains(weapon.Caliber, term)
            || weapon.Units.Any(unit =>
                Contains(unit.PrimarySerialNumber, term)
                || Contains(unit.Notes, term)
                || unit.Parts.Any(part =>
                    Contains(part.PartName, term)
                    || Contains(part.SerialNumber, term)
                    || Contains(part.Notes, term)));
    }

    private static bool Contains(string? value, string term)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<WeaponRelatedOrderViewModel>> LoadRelatedOrdersAsync(int weaponId, CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllOrdersAsync(cancellationToken);
        return orders
            .Where(order => order.Status != OrderStatus.Created
                && order.Items.Any(item => item.WeaponId == weaponId))
            .OrderByDescending(order => order.CreatedAt)
            .Take(10)
            .Select(order => new WeaponRelatedOrderViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.GetPublicOrderNumber(),
                CustomerName = $"{order.User?.FirstName} {order.User?.LastName}".Trim(),
                CustomerEmail = order.User?.Email ?? string.Empty,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            })
            .ToList();
    }

    private static List<WeaponUnitRowViewModel> BuildUnitRows(Weapon weapon)
    {
        return weapon.Units
            .OrderBy(unit => unit.Status)
            .ThenBy(unit => unit.PrimarySerialNumber)
            .Select(unit => new WeaponUnitRowViewModel
            {
                UnitId = unit.Id,
                PrimarySerialNumber = unit.PrimarySerialNumber,
                Status = unit.Status,
                Notes = unit.Notes,
                ReservedOrderNumber = unit.ReservedOrderId.HasValue ? $"#{unit.ReservedOrderId.Value}" : null,
                SoldOrderNumber = unit.SoldOrderId.HasValue ? $"#{unit.SoldOrderId.Value}" : null,
                PartSummaries = unit.Parts
                    .OrderBy(part => part.SlotNumber)
                    .Select(part => $"{part.PartName}: {part.SerialNumber}")
                    .ToList()
            })
            .ToList();
    }

    private static void PopulateUnitContext(WeaponUnitInputModel model, Weapon weapon, bool isCreate)
    {
        model.WeaponName = weapon.Name;
        model.WeaponCategory = weapon.Category;
        model.WeaponTypeDesignation = weapon.TypeDesignation;
        model.IsCreate = isCreate;
    }

    private static void PopulateUnitsBatchContext(WeaponUnitsBatchInputModel model, Weapon weapon)
    {
        model.WeaponName = weapon.Name;
        model.WeaponCategory = weapon.Category;
        model.WeaponTypeDesignation = weapon.TypeDesignation;
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

    private static WeaponUnit MapUnit(WeaponUnitInputModel model)
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
