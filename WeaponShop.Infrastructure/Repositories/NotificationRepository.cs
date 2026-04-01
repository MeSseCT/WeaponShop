using Microsoft.EntityFrameworkCore;
using System.Linq;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;

namespace WeaponShop.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task<List<Notification>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
