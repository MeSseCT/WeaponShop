using Microsoft.AspNetCore.Http;

namespace WeaponShop.Web.ViewModels.Account;

public class DocumentsViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public IFormFile? IdCardFile { get; set; }
    public IFormFile? PurchasePermitFile { get; set; }
    public bool RemoveIdCard { get; set; }
    public bool RemovePurchasePermit { get; set; }
    public bool HasIdCard { get; set; }
    public bool HasPurchasePermit { get; set; }
    public bool IdCardIssuedInCzechRepublic { get; set; }
    public bool FirearmsLicenseRecorded { get; set; }
    public DateTimeOffset? DocumentsUpdatedAt { get; set; }
    public bool IsAdult { get; set; }
    public bool IsProfileReadyForReview { get; set; }
}
