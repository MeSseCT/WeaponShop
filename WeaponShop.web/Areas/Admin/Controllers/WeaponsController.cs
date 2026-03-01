using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeaponShop.Application.Services;
using WeaponShop.Domain;
using WeaponShop.Web.ViewModels.Weapons;

namespace WeaponShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
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
        return View(new WeaponInputModel());
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Create(WeaponInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var weapon = MapToEntity(model);
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

        return View(MapToInputModel(weapon));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, WeaponInputModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var weapon = MapToEntity(model);
        await _weaponService.UpdateAsync(weapon, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
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
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        await _weaponService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
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
            Manufacturer = weapon.Manufacturer
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
            Manufacturer = model.Manufacturer
        };
    }
}
