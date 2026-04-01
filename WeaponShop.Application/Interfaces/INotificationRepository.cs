using WeaponShop.Domain;

namespace WeaponShop.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
