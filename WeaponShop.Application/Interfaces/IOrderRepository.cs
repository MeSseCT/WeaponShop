using WeaponShop.Domain;

namespace WeaponShop.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
