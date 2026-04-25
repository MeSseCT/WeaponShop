using WeaponShop.Domain;

namespace WeaponShop.Application.Services;

public interface IAccessoryService
{
    Task<List<Accessory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<AccessoryNavigationItem>> GetAvailableNavigationItemsAsync(CancellationToken cancellationToken = default);
    Task<Accessory?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Accessory accessory, CancellationToken cancellationToken = default);
    Task UpdateAsync(Accessory accessory, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
