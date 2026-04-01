using WeaponShop.Domain.Identity;

namespace WeaponShop.Application.Interfaces;

public interface IApplicationUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
}
