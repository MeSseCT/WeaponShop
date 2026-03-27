using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeaponShop.Domain.Identity;
using WeaponShop.Web.ViewModels.Account;

namespace WeaponShop.Web.Controllers;

public class AccountController : Controller
{
    private const long MaxDocumentSizeBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;
    private readonly IWebHostEnvironment _environment;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _claimsPrincipalFactory = claimsPrincipalFactory;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var principal = await _claimsPrincipalFactory.CreateAsync(user);
        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        var principal = await _claimsPrincipalFactory.CreateAsync(user);
        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> Documents()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = new DocumentsViewModel
        {
            HasIdCard = !string.IsNullOrWhiteSpace(user.IdCardFileName),
            HasGunLicense = !string.IsNullOrWhiteSpace(user.GunLicenseFileName),
            DocumentsUpdatedAt = user.DocumentsUpdatedAt
        };

        return View(model);
    }

    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Documents(DocumentsViewModel model, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (model.IdCardFile is null && model.GunLicenseFile is null)
        {
            ModelState.AddModelError(string.Empty, "Upload at least one document.");
        }

        if (model.IdCardFile is not null)
        {
            if (!TryValidateDocument(model.IdCardFile))
            {
                model.HasIdCard = !string.IsNullOrWhiteSpace(user.IdCardFileName);
                model.HasGunLicense = !string.IsNullOrWhiteSpace(user.GunLicenseFileName);
                model.DocumentsUpdatedAt = user.DocumentsUpdatedAt;
                return View(model);
            }

            user.IdCardFileName = await SaveDocumentAsync(user.Id, model.IdCardFile, "idcard", cancellationToken);
        }

        if (model.GunLicenseFile is not null)
        {
            if (!TryValidateDocument(model.GunLicenseFile))
            {
                model.HasIdCard = !string.IsNullOrWhiteSpace(user.IdCardFileName);
                model.HasGunLicense = !string.IsNullOrWhiteSpace(user.GunLicenseFileName);
                model.DocumentsUpdatedAt = user.DocumentsUpdatedAt;
                return View(model);
            }

            user.GunLicenseFileName = await SaveDocumentAsync(user.Id, model.GunLicenseFile, "gunlicense", cancellationToken);
        }

        if (!ModelState.IsValid)
        {
            model.HasIdCard = !string.IsNullOrWhiteSpace(user.IdCardFileName);
            model.HasGunLicense = !string.IsNullOrWhiteSpace(user.GunLicenseFileName);
            model.DocumentsUpdatedAt = user.DocumentsUpdatedAt;
            return View(model);
        }

        user.DocumentsUpdatedAt = DateTimeOffset.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.HasIdCard = !string.IsNullOrWhiteSpace(user.IdCardFileName);
            model.HasGunLicense = !string.IsNullOrWhiteSpace(user.GunLicenseFileName);
            model.DocumentsUpdatedAt = user.DocumentsUpdatedAt;
            return View(model);
        }

        TempData["StatusMessage"] = "Documents were uploaded successfully.";
        return RedirectToAction(nameof(Documents));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> DownloadDocument(string userId, string type, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Challenge();
        }

        if (!User.IsInRole("Admin") && !string.Equals(currentUserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser is null)
        {
            return NotFound();
        }

        var fileName = type.ToLowerInvariant() switch
        {
            "idcard" => targetUser.IdCardFileName,
            "gunlicense" => targetUser.GunLicenseFileName,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return NotFound();
        }

        var filePath = Path.Combine(GetUserDocumentsDirectory(targetUser.Id), fileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var downloadName = $"{type}-{targetUser.Id}{Path.GetExtension(fileName)}";
        return PhysicalFile(filePath, "application/octet-stream", downloadName);
    }

    private bool TryValidateDocument(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(string.Empty, $"Unsupported file type '{extension}'. Allowed: PDF, JPG, JPEG, PNG.");
            return false;
        }

        if (file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Uploaded file is empty.");
            return false;
        }

        if (file.Length > MaxDocumentSizeBytes)
        {
            ModelState.AddModelError(string.Empty, "Document size cannot exceed 5 MB.");
            return false;
        }

        return true;
    }

    private async Task<string> SaveDocumentAsync(
        string userId,
        IFormFile file,
        string prefix,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{prefix}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        var directory = GetUserDocumentsDirectory(userId);

        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream, cancellationToken);

        return fileName;
    }

    private string GetUserDocumentsDirectory(string userId)
    {
        return Path.Combine(_environment.ContentRootPath, "UserDocuments", userId);
    }
}
