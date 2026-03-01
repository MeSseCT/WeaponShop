using Microsoft.AspNetCore.Mvc;
using WeaponShop.Application.Services;

namespace WeaponShop.Web.Controllers;

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
}
