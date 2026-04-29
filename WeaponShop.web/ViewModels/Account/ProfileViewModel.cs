using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Web.ViewModels.Account;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Jméno je povinné.")]
    [Display(Name = "Jméno")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Příjmení je povinné.")]
    [Display(Name = "Příjmení")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail je povinný.")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Datum narození je povinné.")]
    [DataType(DataType.Date)]
    [Display(Name = "Datum narození")]
    public DateOnly? DateOfBirth { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Současné heslo")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Nové heslo")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Nová hesla se neshodují.")]
    [Display(Name = "Potvrzení nového hesla")]
    public string? ConfirmNewPassword { get; set; }

    public bool HasIdCard { get; set; }
    public bool HasPurchasePermit { get; set; }
    public bool IdCardIssuedInCzechRepublic { get; set; }
    public bool FirearmsLicenseRecorded { get; set; }
    public DateTimeOffset? DocumentsUpdatedAt { get; set; }
}
