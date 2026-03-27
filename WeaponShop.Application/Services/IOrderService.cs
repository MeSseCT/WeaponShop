using WeaponShop.Domain;

namespace WeaponShop.Application.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string userId, CancellationToken cancellationToken = default);
    Task<Order?> GetCurrentOrderAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetSubmittedOrdersAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetAwaitingApprovalOrdersAsync(CancellationToken cancellationToken = default);
    Task<Order> AddItemAsync(int orderId, int weaponId, int quantity, CancellationToken cancellationToken = default);
    Task<Order> AddItemToCurrentOrderAsync(string userId, int weaponId, int quantity, CancellationToken cancellationToken = default);
    Task<Order> RemoveItemFromCurrentOrderAsync(string userId, int weaponId, CancellationToken cancellationToken = default);
    Task<Order> CheckoutCurrentOrderAsync(string userId, CancellationToken cancellationToken = default);
    Task<Order> ApproveOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order> RejectOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order> RecalculateTotalPriceAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order> ChangeStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default);
}
