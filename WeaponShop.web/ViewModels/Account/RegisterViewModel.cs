using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Jméno je povinné.")]
    [StringLength(100)]
    [Display(Name = "Jméno")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Příjmení je povinné.")]
    [StringLength(100)]
    [Display(Name = "Příjmení")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Datum narození je povinné.")]
    [DataType(DataType.Date)]
    [Display(Name = "Datum narození")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "E-mail je povinný.")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Heslo je povinné.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Heslo musí mít alespoň 8 znaků.")]
    [Display(Name = "Heslo")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potvrzení hesla je povinné.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Hesla se neshodují.")]
    [Display(Name = "Potvrzení hesla")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
