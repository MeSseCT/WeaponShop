using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;

namespace WeaponShop.Application.Services;

/// <summary>
/// Default implementation of the weapon service.
/// Contains core business rules for weapon management.
/// </summary>
public class WeaponService : IWeaponService
{
    private readonly IWeaponRepository _repository;

    public WeaponService(IWeaponRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Weapon>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Read operations are delegated to the repository.
        return _repository.GetAllAsync(cancellationToken);
    }

    public Task<List<WeaponNavigationItem>> GetAvailableNavigationItemsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAvailableNavigationItemsAsync(cancellationToken);
    }

    public Task<Weapon?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task<WeaponUnit?> GetUnitByIdAsync(int weaponId, int unitId, CancellationToken cancellationToken = default)
    {
        return _repository.GetUnitByIdAsync(weaponId, unitId, cancellationToken);
    }

    public Task AddAsync(Weapon weapon, CancellationToken cancellationToken = default)
    {
        // Here is a good place for future business validation.
        return _repository.AddAsync(weapon, cancellationToken);
    }

    public Task UpdateAsync(Weapon weapon, CancellationToken cancellationToken = default)
    {
        return _repository.UpdateAsync(weapon, cancellationToken);
    }

    public Task AddUnitAsync(int weaponId, WeaponUnit unit, CancellationToken cancellationToken = default)
    {
        return _repository.AddUnitAsync(weaponId, unit, cancellationToken);
    }

    public Task UpdateUnitAsync(int weaponId, WeaponUnit unit, CancellationToken cancellationToken = default)
    {
        return _repository.UpdateUnitAsync(weaponId, unit, cancellationToken);
    }

    public Task DeleteUnitAsync(int weaponId, int unitId, CancellationToken cancellationToken = default)
    {
        return _repository.DeleteUnitAsync(weaponId, unitId, cancellationToken);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.DeleteAsync(id, cancellationToken);
    }
}
