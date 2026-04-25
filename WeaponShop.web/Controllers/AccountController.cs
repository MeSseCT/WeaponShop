using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Security.Claims;
using System.Security.Cryptography;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain.Identity;
using WeaponShop.Web.ViewModels.Account;

namespace WeaponShop.Web.Controllers;

public class AccountController : Controller
{
    private const long MaxDocumentSizeBytes = 5 * 1024 * 1024;
    private const int MaxDocumentUploadsPerDay = 3;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly IDataProtector _documentProtector;
    private readonly INotificationRepository _notificationRepository;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory,
        IWebHostEnvironment environment,
        IDataProtectionProvider dataProtectionProvider,
        INotificationRepository notificationRepository)
    {
        _userManager = userManager;
        _claimsPrincipalFactory = claimsPrincipalFactory;
        _environment = environment;
        _documentProtector = dataProtectionProvider.CreateProtector("UserDocuments.v1");
        _notificationRepository = notificationRepository;
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
            ModelState.AddModelError(string.Empty, "Neplatný pokus o přihlášení.");
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

        return RedirectToRoleLanding(principal);
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

        if (!model.DateOfBirth.HasValue || !IsAdult(model.DateOfBirth.Value))
        {
            ModelState.AddModelError(nameof(model.DateOfBirth), "Musíte být starší 18 let.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth.Value,
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
    public IActionResult Documents()
    {
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(BuildProfileViewModel(user));
    }

    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var dateOfBirth = model.DateOfBirth;
        if (!dateOfBirth.HasValue)
        {
            ModelState.AddModelError(nameof(model.DateOfBirth), "Datum narození je povinné.");
        }
        else if (!IsAdult(dateOfBirth.Value))
        {
            ModelState.AddModelError(nameof(model.DateOfBirth), "Musíte být starší 18 let.");
        }

        if (!ModelState.IsValid)
        {
            model.HasIdCard = !string.IsNullOrWhiteSpace(user.IdCardFileName);
            model.HasDriverLicense = !string.IsNullOrWhiteSpace(user.DriverLicenseFileName);
            model.DocumentsUpdatedAt = user.DocumentsUpdatedAt;
            return View(model);
        }

        user.DateOfBirth = dateOfBirth ?? throw new InvalidOperationException("Datum narození je povinné.");
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.HasIdCard = !string.IsNullOrWhiteSpace(user.IdCardFileName);
            model.HasDriverLicense = !string.IsNullOrWhiteSpace(user.DriverLicenseFileName);
            model.DocumentsUpdatedAt = user.DocumentsUpdatedAt;
            return View(model);
        }

        TempData["StatusMessage"] = "Profil byl aktualizován.";
        return RedirectToAction(nameof(Profile));
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

        var originalIdCardFileName = user.IdCardFileName;
        var originalDriverLicenseFileName = user.DriverLicenseFileName;
        var originalDocumentsUpdatedAt = user.DocumentsUpdatedAt;
        var originalUploadWindowStartedAtUtc = user.DocumentsUploadWindowStartedAtUtc;
        var originalUploadCount = user.DocumentsUploadCount;
        var savedDocuments = new List<SavedDocument>();

        if (model.IdCardFile is null && model.DriverLicenseFile is null)
        {
            ModelState.AddModelError(string.Empty, "Nahrajte alespoň jeden doklad.");
        }

        if (model.IdCardFile is not null && !TryValidateDocument(model.IdCardFile))
        {
            return View("Profile", BuildProfileViewModel(user));
        }

        if (model.DriverLicenseFile is not null && !TryValidateDocument(model.DriverLicenseFile))
        {
            return View("Profile", BuildProfileViewModel(user));
        }

        if (!ModelState.IsValid)
        {
            return View("Profile", BuildProfileViewModel(user));
        }

        if (!TryReserveDocumentUploadSlot(user))
        {
            return View("Profile", BuildProfileViewModel(user));
        }

        try
        {
            if (model.IdCardFile is not null)
            {
                var idCardDocument = await SaveDocumentAsync(user.Id, model.IdCardFile, "idcard", cancellationToken);
                savedDocuments.Add(idCardDocument);
                user.IdCardFileName = idCardDocument.FileName;
            }

            if (model.DriverLicenseFile is not null)
            {
                var driverLicenseDocument = await SaveDocumentAsync(user.Id, model.DriverLicenseFile, "driverlicense", cancellationToken);
                savedDocuments.Add(driverLicenseDocument);
                user.DriverLicenseFileName = driverLicenseDocument.FileName;
            }

            user.DocumentsUpdatedAt = DateTimeOffset.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                DeleteSavedDocuments(savedDocuments);
                RestoreDocumentState(
                    user,
                    originalIdCardFileName,
                    originalDriverLicenseFileName,
                    originalDocumentsUpdatedAt,
                    originalUploadWindowStartedAtUtc,
                    originalUploadCount);

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View("Profile", BuildProfileViewModel(user));
            }

            DeleteStoredDocument(user.Id, originalIdCardFileName, user.IdCardFileName);
            DeleteStoredDocument(user.Id, originalDriverLicenseFileName, user.DriverLicenseFileName);
        }
        catch (OperationCanceledException)
        {
            DeleteSavedDocuments(savedDocuments);
            RestoreDocumentState(
                user,
                originalIdCardFileName,
                originalDriverLicenseFileName,
                originalDocumentsUpdatedAt,
                originalUploadWindowStartedAtUtc,
                originalUploadCount);
            throw;
        }
        catch (Exception)
        {
            DeleteSavedDocuments(savedDocuments);
            RestoreDocumentState(
                user,
                originalIdCardFileName,
                originalDriverLicenseFileName,
                originalDocumentsUpdatedAt,
                originalUploadWindowStartedAtUtc,
                originalUploadCount);
            ModelState.AddModelError(string.Empty, "Doklady se nepodařilo uložit. Zkuste to prosím znovu.");
            return View("Profile", BuildProfileViewModel(user));
        }

        TempData["StatusMessage"] = "Doklady byly úspěšně nahrány.";
        return RedirectToAction(nameof(Profile));
    }

    private static ProfileViewModel BuildProfileViewModel(ApplicationUser user)
    {
        return new ProfileViewModel
        {
            DateOfBirth = user.DateOfBirth,
            HasIdCard = !string.IsNullOrWhiteSpace(user.IdCardFileName),
            HasDriverLicense = !string.IsNullOrWhiteSpace(user.DriverLicenseFileName),
            DocumentsUpdatedAt = user.DocumentsUpdatedAt
        };
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

        var document = await TryLoadDocumentAsync(userId, type, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var (bytes, fileName, downloadName) = document.Value;
        var contentType = ResolveContentType(fileName);
        return File(bytes, contentType, downloadName);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ViewDocument(string userId, string type, CancellationToken cancellationToken)
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

        var document = await TryLoadDocumentAsync(userId, type, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var (bytes, fileName, _) = document.Value;
        var contentType = ResolveContentType(fileName);
        return File(bytes, contentType);
    }

    private bool TryValidateDocument(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(string.Empty, $"Nepodporovaný typ souboru '{extension}'. Povolené: PDF, JPG, JPEG, PNG.");
            return false;
        }

        if (file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Nahraný soubor je prázdný.");
            return false;
        }

        if (file.Length > MaxDocumentSizeBytes)
        {
            ModelState.AddModelError(string.Empty, "Velikost dokumentu nesmí překročit 5 MB.");
            return false;
        }

        return true;
    }

    private async Task<SavedDocument> SaveDocumentAsync(
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

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);

        var protectedBytes = _documentProtector.Protect(buffer.ToArray());
        await System.IO.File.WriteAllBytesAsync(filePath, protectedBytes, cancellationToken);

        return new SavedDocument(fileName, filePath);
    }

    private string GetUserDocumentsDirectory(string userId)
    {
        return Path.Combine(_environment.ContentRootPath, "UserDocuments", userId);
    }

    private async Task<(byte[] Bytes, string FileName, string DownloadName)?> TryLoadDocumentAsync(
        string userId,
        string type,
        CancellationToken cancellationToken)
    {
        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser is null)
        {
            return null;
        }

        var fileName = type.ToLowerInvariant() switch
        {
            "idcard" => targetUser.IdCardFileName,
            "driverlicense" => targetUser.DriverLicenseFileName,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var filePath = Path.Combine(GetUserDocumentsDirectory(targetUser.Id), fileName);
        if (!System.IO.File.Exists(filePath))
        {
            return null;
        }

        var decryptedBytes = await TryDecryptDocumentAsync(filePath, fileName, cancellationToken);
        if (decryptedBytes is null)
        {
            return null;
        }

        var downloadName = $"{type}-{targetUser.Id}{Path.GetExtension(fileName)}";
        return (decryptedBytes, fileName, downloadName);
    }

    private async Task<byte[]?> TryDecryptDocumentAsync(
        string filePath,
        string fileName,
        CancellationToken cancellationToken)
    {
        var encryptedBytes = await System.IO.File.ReadAllBytesAsync(filePath, cancellationToken);
        try
        {
            return _documentProtector.Unprotect(encryptedBytes);
        }
        catch (CryptographicException)
        {
            if (!LooksLikePlainDocument(encryptedBytes, fileName))
            {
                return null;
            }

            var upgradedBytes = _documentProtector.Protect(encryptedBytes);
            await System.IO.File.WriteAllBytesAsync(filePath, upgradedBytes, cancellationToken);
            return encryptedBytes;
        }
    }

    private static string ResolveContentType(string fileName)
    {
        return ContentTypeProvider.TryGetContentType(fileName, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private static bool LooksLikePlainDocument(byte[] bytes, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".pdf")
        {
            return bytes.Length > 4 &&
                   bytes[0] == 0x25 &&
                   bytes[1] == 0x50 &&
                   bytes[2] == 0x44 &&
                   bytes[3] == 0x46;
        }

        if (extension is ".jpg" or ".jpeg")
        {
            return bytes.Length > 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        }

        if (extension == ".png")
        {
            return bytes.Length > 8 &&
                   bytes[0] == 0x89 &&
                   bytes[1] == 0x50 &&
                   bytes[2] == 0x4E &&
                   bytes[3] == 0x47 &&
                   bytes[4] == 0x0D &&
                   bytes[5] == 0x0A &&
                   bytes[6] == 0x1A &&
                   bytes[7] == 0x0A;
        }

        return false;
    }

    private static bool IsAdult(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;

        if (today < dateOfBirth.AddYears(age))
        {
            age--;
        }

        return age >= 18;
    }

    private IActionResult RedirectToRoleLanding(ClaimsPrincipal principal)
    {
        if (principal.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Orders", new { area = "Admin" });
        }

        if (principal.IsInRole("Skladnik"))
        {
            return RedirectToAction("Index", "Orders", new { area = "Warehouse" });
        }

        if (principal.IsInRole("Zbrojir"))
        {
            return RedirectToAction("Index", "Orders", new { area = "Gunsmith" });
        }

        return RedirectToAction("Index", "Home");
    }

    private bool TryReserveDocumentUploadSlot(ApplicationUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var windowStart = user.DocumentsUploadWindowStartedAtUtc;

        if (!windowStart.HasValue || DateOnly.FromDateTime(windowStart.Value.UtcDateTime) != today)
        {
            user.DocumentsUploadWindowStartedAtUtc = now;
            user.DocumentsUploadCount = 0;
        }

        if (user.DocumentsUploadCount >= MaxDocumentUploadsPerDay)
        {
            ModelState.AddModelError(string.Empty, "Denní limit nahrání dokladů byl vyčerpán. Zkuste to znovu zítra.");
            return false;
        }

        user.DocumentsUploadCount += 1;
        return true;
    }

    private void RestoreDocumentState(
        ApplicationUser user,
        string? idCardFileName,
        string? driverLicenseFileName,
        DateTimeOffset? documentsUpdatedAt,
        DateTimeOffset? documentsUploadWindowStartedAtUtc,
        int documentsUploadCount)
    {
        user.IdCardFileName = idCardFileName;
        user.DriverLicenseFileName = driverLicenseFileName;
        user.DocumentsUpdatedAt = documentsUpdatedAt;
        user.DocumentsUploadWindowStartedAtUtc = documentsUploadWindowStartedAtUtc;
        user.DocumentsUploadCount = documentsUploadCount;
    }

    private void DeleteSavedDocuments(IEnumerable<SavedDocument> savedDocuments)
    {
        foreach (var savedDocument in savedDocuments)
        {
            try
            {
                if (System.IO.File.Exists(savedDocument.FilePath))
                {
                    System.IO.File.Delete(savedDocument.FilePath);
                }
            }
            catch
            {
                // Ignore cleanup failures and keep the original upload error visible to the user.
            }
        }
    }

    private void DeleteStoredDocument(string userId, string? previousFileName, string? currentFileName)
    {
        if (string.IsNullOrWhiteSpace(previousFileName) ||
            string.Equals(previousFileName, currentFileName, StringComparison.Ordinal))
        {
            return;
        }

        var previousFilePath = Path.Combine(GetUserDocumentsDirectory(userId), previousFileName);
        try
        {
            if (System.IO.File.Exists(previousFilePath))
            {
                System.IO.File.Delete(previousFilePath);
            }
        }
        catch
        {
            // The new document is already persisted. Keep the request successful even if old file cleanup fails.
        }
    }

    private sealed record SavedDocument(string FileName, string FilePath);
}
