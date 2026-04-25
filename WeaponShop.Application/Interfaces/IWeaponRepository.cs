using WeaponShop.Application.Services;
using WeaponShop.Domain;

namespace WeaponShop.Application.Interfaces;

/// <summary>
/// Abstraction over data access for weapons.
/// Implementation lives in the Infrastructure layer.
/// </summary>
public interface IWeaponRepository
{
    Task<List<Weapon>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<WeaponNavigationItem>> GetAvailableNavigationItemsAsync(CancellationToken cancellationToken = default);
    Task<Weapon?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Weapon>> GetByIdsForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Weapon weapon, CancellationToken cancellationToken = default);
    Task UpdateAsync(Weapon weapon, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
