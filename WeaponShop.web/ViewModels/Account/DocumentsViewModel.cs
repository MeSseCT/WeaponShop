using Microsoft.AspNetCore.Http;

namespace WeaponShop.Web.ViewModels.Account;

public class DocumentsViewModel
{
    public IFormFile? IdCardFile { get; set; }
    public IFormFile? GunLicenseFile { get; set; }
    public bool HasIdCard { get; set; }
    public bool HasGunLicense { get; set; }
    public DateTimeOffset? DocumentsUpdatedAt { get; set; }
}
