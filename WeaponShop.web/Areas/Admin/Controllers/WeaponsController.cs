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
    private readonly IWebHostEnvironment _environment;

    public WeaponsController(IWeaponService weaponService, IWebHostEnvironment environment)
    {
        _weaponService = weaponService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var weapons = await _weaponService.GetAllAsync(cancellationToken);
        return View(weapons);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ViewData["FormMode"] = "Create";
        return View(new WeaponInputModel());
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
        return View(MapToInputModel(weapon));
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
            return View(model);
        }

        if (IsSkladnikOnly())
        {
            existing.StockQuantity = model.StockQuantity;
            existing.IsAvailable = model.IsAvailable;
        }
        else
        {
            existing.Name = model.Name;
            existing.Category = model.Category;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.Manufacturer = model.Manufacturer;
        }

        existing.StockQuantity = model.StockQuantity;
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
        return RedirectToAction(nameof(Index));
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

    private static WeaponInputModel MapToInputModel(Weapon weapon)
    {
        return new WeaponInputModel
        {
            Id = weapon.Id,
            Name = weapon.Name,
            Category = weapon.Category,
            Description = weapon.Description,
            Price = weapon.Price,
            Manufacturer = weapon.Manufacturer,
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
            Category = model.Category,
            Description = model.Description,
            Price = model.Price,
            Manufacturer = model.Manufacturer,
            StockQuantity = model.StockQuantity,
            IsAvailable = model.IsAvailable
        };
    }

    private static void ApplyImageState(WeaponInputModel model, Weapon weapon)
    {
        model.CurrentImagePath = CatalogImageStorage.ToPublicPath(weapon.ImageFileName);
        model.HasImage = !string.IsNullOrWhiteSpace(weapon.ImageFileName);
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
}
