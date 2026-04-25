using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;

namespace WeaponShop.Application.Services;

public class AccessoryService : IAccessoryService
{
    private readonly IAccessoryRepository _repository;

    public AccessoryService(IAccessoryRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Accessory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public Task<List<AccessoryNavigationItem>> GetAvailableNavigationItemsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAvailableNavigationItemsAsync(cancellationToken);
    }

    public Task<Accessory?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task AddAsync(Accessory accessory, CancellationToken cancellationToken = default)
    {
        return _repository.AddAsync(accessory, cancellationToken);
    }

    public Task UpdateAsync(Accessory accessory, CancellationToken cancellationToken = default)
    {
        return _repository.UpdateAsync(accessory, cancellationToken);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return _repository.DeleteAsync(id, cancellationToken);
    }
}
