using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WeaponShop.Web.ViewModels.Account;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    public bool HasIdCard { get; set; }
    public bool HasDriverLicense { get; set; }
    public DateTimeOffset? DocumentsUpdatedAt { get; set; }

    public IFormFile? IdCardFile { get; set; }
    public IFormFile? DriverLicenseFile { get; set; }
}
