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
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
