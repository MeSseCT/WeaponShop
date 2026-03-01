using WeaponShop.Domain;

namespace WeaponShop.Application.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string userId, CancellationToken cancellationToken = default);
    Task<Order> AddItemAsync(int orderId, int weaponId, int quantity, CancellationToken cancellationToken = default);
    Task<Order> RecalculateTotalPriceAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order> ChangeStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default);
}
