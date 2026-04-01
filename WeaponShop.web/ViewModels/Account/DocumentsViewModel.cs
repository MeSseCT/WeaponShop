using Microsoft.AspNetCore.Http;

namespace WeaponShop.Web.ViewModels.Account;

public class DocumentsViewModel
{
    public IFormFile? IdCardFile { get; set; }
    public IFormFile? DriverLicenseFile { get; set; }
    public bool HasIdCard { get; set; }
    public bool HasDriverLicense { get; set; }
    public DateTimeOffset? DocumentsUpdatedAt { get; set; }
}
