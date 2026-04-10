using System.ComponentModel.DataAnnotations;

namespace WeaponShop.Web.ViewModels.Cart;

public class CheckoutInputModel
{
    [Required(ErrorMessage = "Vyplňte kontaktní e-mail.")]
    [EmailAddress(ErrorMessage = "Zadejte platný e-mail.")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vyplňte kontaktní telefon.")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vyberte způsob doručení.")]
    public string DeliveryMethod { get; set; } = "pickup";

    [Required(ErrorMessage = "Vyberte způsob platby.")]
    public string PaymentMethod { get; set; } = "bank-transfer";

    [Required(ErrorMessage = "Vyplňte jméno příjemce.")]
    public string ShippingName { get; set; } = string.Empty;

    public string ShippingStreet { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vyplňte fakturační jméno.")]
    public string BillingName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vyplňte fakturační ulici.")]
    public string BillingStreet { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vyplňte fakturační město.")]
    public string BillingCity { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vyplňte PSČ.")]
    public string BillingPostalCode { get; set; } = string.Empty;

    public string CustomerNote { get; set; } = string.Empty;
    public bool BillingSameAsShipping { get; set; } = true;
}
