using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Web.ViewModels.Admin;

public class StaffUserCreateViewModel
{
    [Required(ErrorMessage = "Jméno je povinné.")]
    [StringLength(100)]
    [Display(Name = "Jméno")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Příjmení je povinné.")]
    [StringLength(100)]
    [Display(Name = "Příjmení")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail je povinný.")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Heslo je povinné.")]
    [DataType(DataType.Password)]
    [Display(Name = "Heslo")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potvrzení hesla je povinné.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Hesla se neshodují.")]
    [Display(Name = "Potvrzení hesla")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role je povinná.")]
    [Display(Name = "Interní role")]
    public string Role { get; set; } = "Skladnik";
}
