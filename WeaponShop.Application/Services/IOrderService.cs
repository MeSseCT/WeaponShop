using WeaponShop.Domain;

namespace WeaponShop.Application.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string userId, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetCurrentOrderAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetSubmittedOrdersAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetUserHistoryAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
    Task<List<OrderAudit>> GetAuditsByActorAsync(string actorUserId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetAwaitingApprovalOrdersAsync(CancellationToken cancellationToken = default);
    Task<List<Order>> GetWarehouseOrdersAsync(CancellationToken cancellationToken = default);
    Task<List<Order>> GetGunsmithOrdersAsync(CancellationToken cancellationToken = default);
    Task<Order> AddItemAsync(int orderId, int weaponId, int quantity, CancellationToken cancellationToken = default);
    Task<Order> AddItemToCurrentOrderAsync(string userId, int weaponId, int quantity, CancellationToken cancellationToken = default);
    Task<Order> AddAccessoryItemToCurrentOrderAsync(string userId, int accessoryId, int quantity, CancellationToken cancellationToken = default);
    Task<Order> RemoveItemFromCurrentOrderAsync(string userId, int weaponId, CancellationToken cancellationToken = default);
    Task<Order> RemoveAccessoryItemFromCurrentOrderAsync(string userId, int accessoryId, CancellationToken cancellationToken = default);
    Task<Order> CheckoutCurrentOrderAsync(string userId, CheckoutDetails checkoutDetails, CancellationToken cancellationToken = default);
    Task<Order> ApproveOrderAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default);
    Task<Order> RejectOrderAsync(int orderId, string actorUserId, string? actorName, string? actorRole, string? reason, CancellationToken cancellationToken = default);
    Task<Order> MarkWarehouseCheckedAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default);
    Task<Order> MarkGunsmithCheckedAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default);
    Task<Order> MarkShippedAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default);
    Task<Order> MarkReadyForPickupAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default);
    Task<Order> MarkPickupHandedOverAsync(int orderId, string actorUserId, string? actorName, string? actorRole, CancellationToken cancellationToken = default);
    Task<Order> RecalculateTotalPriceAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order> ChangeStatusAsync(int orderId, OrderStatus status, string actorUserId, string? actorName, string? actorRole, string? notes, CancellationToken cancellationToken = default);
    Task DeleteOrderAsync(int orderId, CancellationToken cancellationToken = default);
}
