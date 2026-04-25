using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WeaponShop.Domain.Identity;
using WeaponShop.Web.ViewModels.Admin;

namespace WeaponShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private static readonly string[] InternalRoles = ["Admin", "Skladnik", "Zbrojir"];

    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var result = new List<StaffUserListItemViewModel>();
        foreach (var user in users)
        {
            var role = await GetPrimaryInternalRoleAsync(user);
            if (role is null)
            {
                continue;
            }

            result.Add(new StaffUserListItemViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Role = role,
                IsActive = IsActive(user),
                IsCurrentAdmin = string.Equals(user.Id, GetCurrentUserId(), StringComparison.Ordinal)
            });
        }

        return View(result);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new StaffUserCreateViewModel());
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Create(StaffUserCreateViewModel model)
    {
        if (!InternalRoles.Contains(model.Role, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Role), "Vyberte platnou interní roli.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Uživatel s tímto e-mailem již existuje.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim()
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await _userManager.DeleteAsync(user);
            return View(model);
        }

        if (await _userManager.IsInRoleAsync(user, "Customer"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Customer");
        }

        TempData["StatusMessage"] = $"Interní uživatel {model.Email} byl vytvořen s rolí {model.Role}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var role = await GetPrimaryInternalRoleAsync(user);
        if (role is null)
        {
            return NotFound();
        }

        return View(new StaffUserEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Role = role,
            IsActive = IsActive(user),
            IsCurrentAdmin = string.Equals(user.Id, GetCurrentUserId(), StringComparison.Ordinal)
        });
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Edit(string id, StaffUserEditViewModel model)
    {
        if (!string.Equals(id, model.Id, StringComparison.Ordinal))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var currentRole = await GetPrimaryInternalRoleAsync(user);
        if (currentRole is null)
        {
            return NotFound();
        }

        var isCurrentAdmin = string.Equals(user.Id, GetCurrentUserId(), StringComparison.Ordinal);
        if (!InternalRoles.Contains(model.Role, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Role), "Vyberte platnou interní roli.");
        }

        if (isCurrentAdmin && !string.Equals(model.Role, currentRole, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Role), "Vlastní administrátorskou roli nelze změnit z tohoto účtu.");
        }

        if (!string.IsNullOrWhiteSpace(model.Password) && string.IsNullOrWhiteSpace(model.ConfirmPassword))
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Potvrďte nové heslo.");
        }

        if (!ModelState.IsValid)
        {
            model.IsActive = IsActive(user);
            model.IsCurrentAdmin = isCurrentAdmin;
            return View(model);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.NormalizedEmail = _userManager.NormalizeEmail(user.Email);
        user.NormalizedUserName = _userManager.NormalizeName(user.UserName);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.IsActive = IsActive(user);
            model.IsCurrentAdmin = isCurrentAdmin;
            return View(model);
        }

        if (!string.Equals(currentRole, model.Role, StringComparison.Ordinal))
        {
            var roles = await _userManager.GetRolesAsync(user);
            var internalRoles = roles.Where(role => InternalRoles.Contains(role, StringComparer.Ordinal)).ToArray();
            if (internalRoles.Length > 0)
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, internalRoles);
                if (!removeRolesResult.Succeeded)
                {
                    foreach (var error in removeRolesResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    model.IsActive = IsActive(user);
                    model.IsCurrentAdmin = isCurrentAdmin;
                    return View(model);
                }
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!addRoleResult.Succeeded)
            {
                foreach (var error in addRoleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.IsActive = IsActive(user);
                model.IsCurrentAdmin = isCurrentAdmin;
                return View(model);
            }
        }

        if (await _userManager.IsInRoleAsync(user, "Customer"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Customer");
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.IsActive = IsActive(user);
                model.IsCurrentAdmin = isCurrentAdmin;
                return View(model);
            }
        }

        TempData["StatusMessage"] = $"Interní účet {user.Email} byl aktualizován.";
        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var role = await GetPrimaryInternalRoleAsync(user);
        if (role is null)
        {
            return NotFound();
        }

        if (string.Equals(user.Id, GetCurrentUserId(), StringComparison.Ordinal))
        {
            TempData["ErrorMessage"] = "Vlastní administrátorský účet nelze deaktivovat.";
            return RedirectToAction(nameof(Index));
        }

        IdentityResult result;
        if (IsActive(user))
        {
            result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            if (result.Succeeded)
            {
                user.LockoutEnabled = true;
                result = await _userManager.UpdateAsync(user);
            }

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = $"Účet {user.Email} byl deaktivován.";
            }
        }
        else
        {
            user.LockoutEnabled = true;
            result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.SetLockoutEndDateAsync(user, null);
            }

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = $"Účet {user.Email} byl znovu aktivován.";
            }
        }

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(error => error.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var role = await GetPrimaryInternalRoleAsync(user);
        if (role is null)
        {
            return NotFound();
        }

        if (string.Equals(user.Id, GetCurrentUserId(), StringComparison.Ordinal))
        {
            TempData["ErrorMessage"] = "Vlastní administrátorský účet nelze smazat.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["StatusMessage"] = $"Účet {user.Email} byl odstraněn.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string?> GetPrimaryInternalRoleAsync(ApplicationUser user)
    {
        foreach (var role in InternalRoles)
        {
            if (await _userManager.IsInRoleAsync(user, role))
            {
                return role;
            }
        }

        return null;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static bool IsActive(ApplicationUser user)
    {
        return !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow;
    }
}
