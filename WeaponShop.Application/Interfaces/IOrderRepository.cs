using WeaponShop.Domain;

namespace WeaponShop.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetCurrentByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Order>> GetAwaitingApprovalAsync(CancellationToken cancellationToken = default);
    Task<List<Order>> GetByStatusesAsync(IReadOnlyCollection<OrderStatus> statuses, CancellationToken cancellationToken = default);
    Task<List<OrderAudit>> GetAuditsByActorAsync(string actorUserId, CancellationToken cancellationToken = default);
    void Remove(Order order);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task AddAuditAsync(OrderAudit audit, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
