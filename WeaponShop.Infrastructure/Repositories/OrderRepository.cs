using Microsoft.EntityFrameworkCore;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;

namespace WeaponShop.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(order => order.User)
            .Include(order => order.Items)
            .ThenInclude(item => item.Weapon)
            .Include(order => order.Items)
            .ThenInclude(item => item.Accessory)
            .Include(order => order.Audits)
            .Include(order => order.Notifications)
            .AsSplitQuery()
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public async Task<Order?> GetCurrentByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(order => order.User)
            .Include(order => order.Items)
            .ThenInclude(item => item.Weapon)
            .Include(order => order.Items)
            .ThenInclude(item => item.Accessory)
            .Include(order => order.Audits)
            .Include(order => order.Notifications)
            .AsSplitQuery()
            .Where(order => order.UserId == userId && order.Status == OrderStatus.Created)
            .OrderByDescending(order => order.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(order => order.User)
            .Include(order => order.Items)
            .ThenInclude(item => item.Weapon)
            .Include(order => order.Items)
            .ThenInclude(item => item.Accessory)
            .Include(order => order.Audits)
            .Include(order => order.Notifications)
            .AsSplitQuery()
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(order => order.User)
            .Include(order => order.Items)
            .ThenInclude(item => item.Weapon)
            .Include(order => order.Items)
            .ThenInclude(item => item.Accessory)
            .Include(order => order.Audits)
            .Include(order => order.Notifications)
            .AsSplitQuery()
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> GetAwaitingApprovalAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(order => order.User)
            .Include(order => order.Items)
            .ThenInclude(item => item.Weapon)
            .Include(order => order.Items)
            .ThenInclude(item => item.Accessory)
            .Include(order => order.Audits)
            .Include(order => order.Notifications)
            .AsSplitQuery()
            .Where(order => order.Status == OrderStatus.AwaitingApproval)
            .OrderBy(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> GetByStatusesAsync(
        IReadOnlyCollection<OrderStatus> statuses,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(order => order.User)
            .Include(order => order.Items)
            .ThenInclude(item => item.Weapon)
            .Include(order => order.Items)
            .ThenInclude(item => item.Accessory)
            .Include(order => order.Audits)
            .Include(order => order.Notifications)
            .AsSplitQuery()
            .Where(order => statuses.Contains(order.Status))
            .OrderBy(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OrderAudit>> GetAuditsByActorAsync(
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.OrderAudits
            .Include(audit => audit.Order)
            .ThenInclude(order => order!.User)
            .Where(audit => audit.ActorUserId == actorUserId)
            .OrderByDescending(audit => audit.OccurredAtUtc)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public void Remove(Order order)
    {
        _context.Orders.Remove(order);
    }

    public async Task AddAuditAsync(OrderAudit audit, CancellationToken cancellationToken = default)
    {
        await _context.OrderAudits.AddAsync(audit, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
