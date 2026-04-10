using WeaponShop.Domain;

namespace WeaponShop.Application.Interfaces;

public interface IAccessoryRepository
{
    Task<List<Accessory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Accessory?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Accessory>> GetByIdsForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Accessory accessory, CancellationToken cancellationToken = default);
    Task UpdateAsync(Accessory accessory, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
