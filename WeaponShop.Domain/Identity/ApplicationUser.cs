using Microsoft.AspNetCore.Identity;
using WeaponShop.Domain;

namespace WeaponShop.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
