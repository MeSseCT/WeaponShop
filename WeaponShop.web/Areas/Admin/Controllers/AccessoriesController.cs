using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Web.Helpers;
using WeaponShop.Web.ViewModels.Accessories;

namespace WeaponShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Skladnik")]
public class AccessoriesController : Controller
{
    private readonly IAccessoryService _accessoryService;
    private readonly IWebHostEnvironment _environment;

    public AccessoriesController(IAccessoryService accessoryService, IWebHostEnvironment environment)
    {
        _accessoryService = accessoryService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accessories = await _accessoryService.GetAllAsync(cancellationToken);
        return View(accessories);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ViewData["FormMode"] = "Create";
        return View(new AccessoryInputModel());
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(AccessoryInputModel model, CancellationToken cancellationToken)
    {
        ViewData["FormMode"] = "Create";
        ValidateImageInputs(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var accessory = MapToEntity(model);

        if (model.ImageFile is not null)
        {
            accessory.ImageFileName = await CatalogImageStorage.SaveAsync(_environment, model.ImageFile, "accessory", cancellationToken);
        }

        if (model.AdditionalImageFiles.Count > 0)
        {
            var sortOrder = 0;
            foreach (var additionalImage in model.AdditionalImageFiles.Where(file => file is not null && file.Length > 0))
            {
                accessory.Images.Add(new AccessoryImage
                {
                    FileName = await CatalogImageStorage.SaveAsync(_environment, additionalImage, "accessory", cancellationToken),
                    SortOrder = sortOrder++
                });
            }
        }

        await _accessoryService.AddAsync(accessory, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var accessory = await _accessoryService.GetByIdAsync(id, cancellationToken);
        if (accessory is null)
        {
            return NotFound();
        }

        ViewData["FormMode"] = "Edit";
        return View(MapToInputModel(accessory));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, AccessoryInputModel model, CancellationToken cancellationToken)
    {
        ViewData["FormMode"] = "Edit";
        if (id != model.Id)
        {
            return BadRequest();
        }

        var existing = await _accessoryService.GetByIdAsync(id, cancellationToken);
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
                existing.ImageFileName = await CatalogImageStorage.SaveAsync(_environment, model.ImageFile, "accessory", cancellationToken);
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
                existing.Images.Add(new AccessoryImage
                {
                    FileName = await CatalogImageStorage.SaveAsync(_environment, additionalImage, "accessory", cancellationToken),
                    SortOrder = nextSortOrder++
                });
            }
        }

        await _accessoryService.UpdateAsync(existing, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var accessory = await _accessoryService.GetByIdAsync(id, cancellationToken);
        if (accessory is null)
        {
            return NotFound();
        }

        return View(accessory);
    }

    [ValidateAntiForgeryToken]
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var existing = await _accessoryService.GetByIdAsync(id, cancellationToken);
        if (existing is not null)
        {
            CatalogImageStorage.DeleteIfExists(_environment, existing.ImageFileName);
            foreach (var galleryImage in existing.Images)
            {
                CatalogImageStorage.DeleteIfExists(_environment, galleryImage.FileName);
            }
        }

        await _accessoryService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private bool IsSkladnikOnly()
    {
        return User.IsInRole("Skladnik") && !User.IsInRole("Admin");
    }

    private bool IsAdminOnly()
    {
        return User.IsInRole("Admin") && !User.IsInRole("Skladnik");
    }

    private static AccessoryInputModel MapToInputModel(Accessory accessory)
    {
        return new AccessoryInputModel
        {
            Id = accessory.Id,
            Name = accessory.Name,
            Category = accessory.Category,
            Description = accessory.Description,
            Price = accessory.Price,
            Manufacturer = accessory.Manufacturer,
            StockQuantity = accessory.StockQuantity,
            IsAvailable = accessory.IsAvailable,
            CurrentImagePath = CatalogImageStorage.ToPublicPath(accessory.ImageFileName),
            HasImage = !string.IsNullOrWhiteSpace(accessory.ImageFileName),
            ExistingGalleryImages = accessory.Images
                .OrderBy(image => image.SortOrder)
                .ThenBy(image => image.Id)
                .Select(image => new AccessoryGalleryImageInputModel
                {
                    Id = image.Id,
                    ImagePath = CatalogImageStorage.ToPublicPath(image.FileName) ?? string.Empty
                })
                .ToList()
        };
    }

    private static Accessory MapToEntity(AccessoryInputModel model)
    {
        return new Accessory
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

    private static void ApplyImageState(AccessoryInputModel model, Accessory accessory)
    {
        model.CurrentImagePath = CatalogImageStorage.ToPublicPath(accessory.ImageFileName);
        model.HasImage = !string.IsNullOrWhiteSpace(accessory.ImageFileName);
        model.ExistingGalleryImages = accessory.Images
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.Id)
            .Select(image => new AccessoryGalleryImageInputModel
            {
                Id = image.Id,
                ImagePath = CatalogImageStorage.ToPublicPath(image.FileName) ?? string.Empty,
                Remove = model.ExistingGalleryImages.FirstOrDefault(item => item.Id == image.Id)?.Remove ?? false
            })
            .ToList();
    }

    private void ValidateImageInputs(AccessoryInputModel model)
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
