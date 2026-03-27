using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;

namespace WeaponShop.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IWeaponRepository _weaponRepository;

    public OrderService(IOrderRepository orderRepository, IWeaponRepository weaponRepository)
    {
        _orderRepository = orderRepository;
        _weaponRepository = weaponRepository;
    }

    public async Task<Order> CreateOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Created,
            TotalPrice = 0m
        };

        await _orderRepository.AddAsync(order, cancellationToken);
        return order;
    }

    public async Task<Order?> GetCurrentOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        return await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken);
    }

    public async Task<Order> AddItemAsync(int orderId, int weaponId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} was not found.");
        }

        var weapon = await _weaponRepository.GetByIdAsync(weaponId, cancellationToken);
        if (weapon is null)
        {
            throw new KeyNotFoundException($"Weapon with ID {weaponId} was not found.");
        }

        var existingItem = order.Items.SingleOrDefault(item => item.WeaponId == weaponId);
        if (existingItem is null)
        {
            order.Items.Add(new OrderItem
            {
                WeaponId = weaponId,
                Quantity = quantity,
                UnitPrice = weapon.Price
            });
        }
        else
        {
            existingItem.Quantity += quantity;
            existingItem.UnitPrice = weapon.Price;
        }

        RecalculateTotalPrice(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<Order> AddItemToCurrentOrderAsync(
        string userId,
        int weaponId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var currentOrder = await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken)
            ?? await CreateOrderAsync(userId, cancellationToken);

        return await AddItemAsync(currentOrder.Id, weaponId, quantity, cancellationToken);
    }

    public async Task<Order> RemoveItemFromCurrentOrderAsync(
        string userId,
        int weaponId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var currentOrder = await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken);
        if (currentOrder is null)
        {
            throw new InvalidOperationException("Current order was not found.");
        }

        var existingItem = currentOrder.Items.SingleOrDefault(item => item.WeaponId == weaponId);
        if (existingItem is null)
        {
            return currentOrder;
        }

        currentOrder.Items.Remove(existingItem);
        RecalculateTotalPrice(currentOrder);
        await _orderRepository.SaveChangesAsync(cancellationToken);
        return currentOrder;
    }

    public async Task<Order> CheckoutCurrentOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var currentOrder = await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken);
        if (currentOrder is null)
        {
            throw new InvalidOperationException("Current order was not found.");
        }

        if (currentOrder.Items.Count == 0)
        {
            throw new InvalidOperationException("Cannot checkout an empty order.");
        }

        currentOrder.Status = OrderStatus.AwaitingApproval;
        await _orderRepository.SaveChangesAsync(cancellationToken);
        return currentOrder;
    }

    public async Task<Order> RecalculateTotalPriceAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} was not found.");
        }

        RecalculateTotalPrice(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<Order> ChangeStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} was not found.");
        }

        order.Status = status;
        await _orderRepository.SaveChangesAsync(cancellationToken);
        return order;
    }

    private static void RecalculateTotalPrice(Order order)
    {
        order.TotalPrice = order.Items.Sum(item => item.UnitPrice * item.Quantity);
    }
}
