using WeaponShop.Domain;

namespace WeaponShop.Application.Services;

/// <summary>
/// Application service for weapon-related operations.
/// Orchestrates business logic and delegates persistence to repositories.
/// </summary>
public interface IWeaponService
{
    Task<List<Weapon>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<WeaponNavigationItem>> GetAvailableNavigationItemsAsync(CancellationToken cancellationToken = default);
    Task<Weapon?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Weapon weapon, CancellationToken cancellationToken = default);
    Task UpdateAsync(Weapon weapon, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
