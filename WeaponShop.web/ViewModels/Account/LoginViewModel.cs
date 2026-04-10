using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-mail je povinný.")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Heslo je povinné.")]
    [DataType(DataType.Password)]
    [Display(Name = "Heslo")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Zapamatovat si mě")]
    public bool RememberMe { get; set; }
}
