using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Web.ViewModels.Weapons;

namespace WeaponShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Skladnik")]
public class WeaponsController : Controller
{
    private readonly IWeaponService _weaponService;

    public WeaponsController(IWeaponService weaponService)
    {
        _weaponService = weaponService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var weapons = await _weaponService.GetAllAsync(cancellationToken);
        return View(weapons);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["FormMode"] = "Create";
        return View(new WeaponInputModel());
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Create(WeaponInputModel model, CancellationToken cancellationToken)
    {
        ViewData["FormMode"] = "Create";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var weapon = MapToEntity(model);
        if (IsAdminOnly())
        {
            weapon.StockQuantity = 0;
            weapon.IsAvailable = false;
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

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _weaponService.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
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
        await _weaponService.DeleteAsync(id, cancellationToken);
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
            IsAvailable = weapon.IsAvailable
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
}
