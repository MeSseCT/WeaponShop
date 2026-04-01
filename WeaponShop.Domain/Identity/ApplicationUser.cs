using Microsoft.AspNetCore.Identity;
using WeaponShop.Domain;

namespace WeaponShop.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? IdCardFileName { get; set; }
    public string? DriverLicenseFileName { get; set; }
    public DateTimeOffset? DocumentsUpdatedAt { get; set; }
    public DateTimeOffset? DocumentsUploadWindowStartedAtUtc { get; set; }
    public int DocumentsUploadCount { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
