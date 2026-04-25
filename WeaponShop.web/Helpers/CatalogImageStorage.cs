using Microsoft.AspNetCore.Http;

namespace WeaponShop.Web.Helpers;

public static class CatalogImageStorage
{
    public const long MaxImageSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public static bool IsValidImage(IFormFile? file, out string? validationError)
    {
        validationError = null;
        if (file is null)
        {
            return true;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            validationError = "Povolené formáty obrázků jsou JPG, JPEG, PNG a WEBP.";
            return false;
        }

        if (file.Length <= 0)
        {
            validationError = "Nahraný obrázek je prázdný.";
            return false;
        }

        if (file.Length > MaxImageSizeBytes)
        {
            validationError = "Velikost obrázku nesmí překročit 5 MB.";
            return false;
        }

        return true;
    }

    public static async Task<string> SaveAsync(
        IWebHostEnvironment environment,
        IFormFile file,
        string prefix,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{prefix}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        var directory = GetStorageDirectory(environment);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, fileName);
        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream, cancellationToken);
        return fileName;
    }

    public static void DeleteIfExists(IWebHostEnvironment environment, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var safeFileName = Path.GetFileName(fileName);
        var filePath = Path.Combine(GetStorageDirectory(environment), safeFileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public static string? ToPublicPath(string? fileName)
    {
        return string.IsNullOrWhiteSpace(fileName)
            ? null
            : $"/images/catalog/uploads/{Path.GetFileName(fileName)}";
    }

    private static string GetStorageDirectory(IWebHostEnvironment environment)
    {
        return Path.Combine(environment.WebRootPath, "images", "catalog", "uploads");
    }
}
