using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;

namespace WeaponShop.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IWeaponRepository _weaponRepository;
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailSender _emailSender;

    public OrderService(
        IOrderRepository orderRepository,
        IWeaponRepository weaponRepository,
        IApplicationUserRepository applicationUserRepository,
        INotificationRepository notificationRepository,
        IEmailSender emailSender)
    {
        _orderRepository = orderRepository;
        _weaponRepository = weaponRepository;
        _applicationUserRepository = applicationUserRepository;
        _notificationRepository = notificationRepository;
        _emailSender = emailSender;
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
        await _orderRepository.SaveChangesAsync(cancellationToken);
        return order;
    }

    public Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetByIdAsync(orderId, cancellationToken);
    }

    public Task<List<OrderAudit>> GetAuditsByActorAsync(string actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new ArgumentException("Actor user ID is required.", nameof(actorUserId));
        }

        return _orderRepository.GetAuditsByActorAsync(actorUserId, cancellationToken);
    }

    public Task<List<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Order?> GetCurrentOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        return await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken);
    }

    public async Task<List<Order>> GetSubmittedOrdersAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var allUserOrders = await _orderRepository.GetByUserIdAsync(userId, cancellationToken);
        return allUserOrders
            .Where(order => order.Status != OrderStatus.Created)
            .ToList();
    }

    public async Task<List<Order>> GetUserHistoryAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        return await _orderRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public Task<List<Order>> GetAwaitingApprovalOrdersAsync(CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetAwaitingApprovalAsync(cancellationToken);
    }

    public Task<List<Order>> GetWarehouseOrdersAsync(CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetByStatusesAsync(
            new[] { OrderStatus.Approved, OrderStatus.AwaitingDispatch, OrderStatus.ReadyForPickup },
            cancellationToken);
    }

    public Task<List<Order>> GetGunsmithOrdersAsync(CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetByStatusesAsync(
            new[] { OrderStatus.AwaitingGunsmith },
            cancellationToken);
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

        if (!weapon.IsAvailable || weapon.StockQuantity <= 0)
        {
            throw new InvalidOperationException("Selected weapon is currently unavailable.");
        }

        var existingItem = order.Items.SingleOrDefault(item => item.WeaponId == weaponId);
        if (existingItem is null)
        {
            if (quantity > weapon.StockQuantity)
            {
                throw new InvalidOperationException("Requested quantity exceeds available stock.");
            }

            order.Items.Add(new OrderItem
            {
                WeaponId = weaponId,
                Quantity = quantity,
                UnitPrice = weapon.Price
            });
        }
        else
        {
            var newQuantity = existingItem.Quantity + quantity;
            if (newQuantity > weapon.StockQuantity)
            {
                throw new InvalidOperationException("Requested quantity exceeds available stock.");
            }

            existingItem.Quantity = newQuantity;
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

        await EnsureUserCanPurchaseAsync(userId, cancellationToken);

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

        await EnsureUserCanPurchaseAsync(userId, cancellationToken);

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

    public Task<Order> ApproveOrderAsync(
        int orderId,
        string actorUserId,
        string? actorName,
        string? actorRole,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(orderId, OrderStatus.Approved, actorUserId, actorName, actorRole, null, cancellationToken);
    }

    public Task<Order> RejectOrderAsync(
        int orderId,
        string actorUserId,
        string? actorName,
        string? actorRole,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(orderId, OrderStatus.Rejected, actorUserId, actorName, actorRole, reason, cancellationToken);
    }

    public Task<Order> MarkWarehouseCheckedAsync(
        int orderId,
        string actorUserId,
        string? actorName,
        string? actorRole,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(orderId, OrderStatus.AwaitingGunsmith, actorUserId, actorName, actorRole, null, cancellationToken);
    }

    public Task<Order> MarkGunsmithCheckedAsync(
        int orderId,
        string actorUserId,
        string? actorName,
        string? actorRole,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(orderId, OrderStatus.AwaitingDispatch, actorUserId, actorName, actorRole, null, cancellationToken);
    }

    public Task<Order> MarkShippedAsync(
        int orderId,
        string actorUserId,
        string? actorName,
        string? actorRole,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(orderId, OrderStatus.Shipped, actorUserId, actorName, actorRole, null, cancellationToken);
    }

    public Task<Order> MarkReadyForPickupAsync(
        int orderId,
        string actorUserId,
        string? actorName,
        string? actorRole,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(orderId, OrderStatus.ReadyForPickup, actorUserId, actorName, actorRole, null, cancellationToken);
    }

    public Task<Order> MarkPickupHandedOverAsync(
        int orderId,
        string actorUserId,
        string? actorName,
        string? actorRole,
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(orderId, OrderStatus.Completed, actorUserId, actorName, actorRole, null, cancellationToken);
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

    public async Task<Order> ChangeStatusAsync(
        int orderId,
        OrderStatus status,
        string actorUserId,
        string? actorName,
        string? actorRole,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} was not found.");
        }

        var now = DateTime.UtcNow;
        var fromStatus = order.Status;

        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new InvalidOperationException("Actor user ID is required for audited status changes.");
        }

        switch (status)
        {
            case OrderStatus.Approved:
                if (order.Status != OrderStatus.AwaitingApproval)
                {
                    throw new InvalidOperationException("Only orders awaiting approval can be approved.");
                }

                order.ApprovedAtUtc = now;
                order.Status = OrderStatus.Approved;
                break;
            case OrderStatus.Rejected:
                if (order.Status is OrderStatus.Rejected or OrderStatus.Completed or OrderStatus.Shipped)
                {
                    throw new InvalidOperationException("This order cannot be rejected at the current stage.");
                }

                order.RejectedAtUtc = now;
                order.Status = OrderStatus.Rejected;
                break;
            case OrderStatus.AwaitingGunsmith:
                if (order.Status != OrderStatus.Approved)
                {
                    throw new InvalidOperationException("Order must be admin approved before warehouse can send it to the gunsmith.");
                }

                order.WarehouseCheckedAtUtc = now;
                order.Status = OrderStatus.AwaitingGunsmith;
                break;
            case OrderStatus.AwaitingDispatch:
                if (order.Status != OrderStatus.AwaitingGunsmith)
                {
                    throw new InvalidOperationException("Order must be with the gunsmith before it can return to the warehouse.");
                }

                order.GunsmithCheckedAtUtc = now;
                order.Status = OrderStatus.AwaitingDispatch;
                break;
            case OrderStatus.Shipped:
                if (order.Status != OrderStatus.AwaitingDispatch)
                {
                    throw new InvalidOperationException("Order must be back at the warehouse before shipping.");
                }

                await ReserveStockIfNeededAsync(order, now, cancellationToken);
                order.WarehousePreparedAtUtc ??= now;
                order.ShippedAtUtc = now;
                order.Status = OrderStatus.Shipped;
                break;
            case OrderStatus.ReadyForPickup:
                if (order.Status != OrderStatus.AwaitingDispatch)
                {
                    throw new InvalidOperationException("Order must be back at the warehouse before pickup is prepared.");
                }

                order.WarehousePreparedAtUtc ??= now;
                order.ReadyForPickupAtUtc = now;
                order.Status = OrderStatus.ReadyForPickup;
                break;
            case OrderStatus.Completed:
                if (order.Status != OrderStatus.ReadyForPickup)
                {
                    throw new InvalidOperationException("Order must be ready for pickup before it can be handed over.");
                }

                await ReserveStockIfNeededAsync(order, now, cancellationToken);
                order.PickupHandedOverAtUtc = now;
                order.Status = OrderStatus.Completed;
                break;
            default:
                order.Status = status;
                break;
        }

        var action = ResolveAuditAction(status, actorRole);
        await _orderRepository.AddAuditAsync(new OrderAudit
        {
            OrderId = order.Id,
            FromStatus = fromStatus,
            ToStatus = order.Status,
            Action = action,
            ActorUserId = actorUserId,
            ActorName = actorName ?? string.Empty,
            ActorRole = actorRole ?? string.Empty,
            OccurredAtUtc = now,
            Notes = notes
        }, cancellationToken);

        if (order.Status is OrderStatus.Approved
            or OrderStatus.AwaitingGunsmith
            or OrderStatus.AwaitingDispatch
            or OrderStatus.Rejected
            or OrderStatus.Shipped
            or OrderStatus.ReadyForPickup
            or OrderStatus.Completed)
        {
            var (title, message) = BuildCustomerNotification(order, now, notes);
            await _notificationRepository.AddAsync(new Notification
            {
                UserId = order.UserId,
                OrderId = order.Id,
                Title = title,
                Message = message,
                CreatedAtUtc = now,
                IsRead = false
            }, cancellationToken);
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);

        if (order.Status is OrderStatus.Shipped or OrderStatus.ReadyForPickup)
        {
            await SendCustomerEmailAsync(order, now, cancellationToken);
        }

        return order;
    }

    public async Task DeleteOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} was not found.");
        }

        if (order.Status == OrderStatus.Created)
        {
            throw new InvalidOperationException("Current cart cannot be deleted from history.");
        }

        _orderRepository.Remove(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    private static string ResolveAuditAction(OrderStatus status, string? actorRole)
    {
        return status switch
        {
            OrderStatus.Approved => "AdminApproved",
            OrderStatus.Rejected => actorRole switch
            {
                "Skladnik" => "WarehouseRejected",
                "Zbrojir" => "GunsmithRejected",
                _ => "AdminRejected"
            },
            OrderStatus.AwaitingGunsmith => "WarehouseChecked",
            OrderStatus.AwaitingDispatch => "GunsmithChecked",
            OrderStatus.Shipped => "OrderShipped",
            OrderStatus.ReadyForPickup => "ReadyForPickup",
            OrderStatus.Completed => "PickupHandedOver",
            _ => $"StatusChanged:{status}"
        };
    }

    private static (string Title, string Message) BuildCustomerNotification(Order order, DateTime now, string? notes)
    {
        var reason = string.IsNullOrWhiteSpace(notes) ? "" : $" Dovod: {notes}";
        return order.Status switch
        {
            OrderStatus.Approved => ($"Objednavka #{order.Id} bola schvalena",
                $"Admin potvrdil doklady a objednavka #{order.Id} bola odoslana skladnikovi ({now:yyyy-MM-dd HH:mm} UTC)."),
            OrderStatus.AwaitingGunsmith => ($"Objednavka #{order.Id} ide k zbrojirovi",
                $"Skladnik potvrdil kontrolu skladu. Objednavka #{order.Id} bola odoslana zbrojirovi ({now:yyyy-MM-dd HH:mm} UTC)."),
            OrderStatus.AwaitingDispatch => ($"Objednavka #{order.Id} vratena na sklad",
                $"Zbrojir dokoncil kontrolu. Objednavka #{order.Id} bola odoslana naspat skladnikovi ({now:yyyy-MM-dd HH:mm} UTC)."),
            OrderStatus.Rejected => ($"Objednavka #{order.Id} bola zamietnuta",
                $"Objednavka #{order.Id} bola zamietnuta ({now:yyyy-MM-dd HH:mm} UTC).{reason}"),
            OrderStatus.Shipped => ($"Objednavka #{order.Id} bola odoslana",
                $"Vasa objednavka #{order.Id} bola odoslana dna {now:yyyy-MM-dd HH:mm} UTC."),
            OrderStatus.ReadyForPickup => ($"Objednavka #{order.Id} je pripravena na vyzdvihnutie",
                $"Vasa objednavka #{order.Id} je pripravena na vyzdvihnutie od {now:yyyy-MM-dd HH:mm} UTC."),
            OrderStatus.Completed => ($"Objednavka #{order.Id} bola prevzata",
                $"Osobny odber bol potvrdeny dna {now:yyyy-MM-dd HH:mm} UTC."),
            _ => ("Objednavka bola aktualizovana", $"Vasa objednavka #{order.Id} bola aktualizovana.")
        };
    }

    private async Task SendCustomerEmailAsync(Order order, DateTime now, CancellationToken cancellationToken)
    {
        var email = order.User?.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var (title, message) = BuildCustomerNotification(order, now, null);
        await _emailSender.SendAsync(email, title, message, cancellationToken);
    }

    private async Task ReserveStockIfNeededAsync(Order order, DateTime now, CancellationToken cancellationToken)
    {
        if (order.StockReservedAtUtc.HasValue)
        {
            return;
        }

        var weaponIds = order.Items.Select(item => item.WeaponId).Distinct().ToList();
        if (weaponIds.Count == 0)
        {
            return;
        }

        var weapons = await _weaponRepository.GetByIdsForUpdateAsync(weaponIds, cancellationToken);
        if (weapons.Count != weaponIds.Count)
        {
            throw new InvalidOperationException("Unable to reserve stock: weapon not found.");
        }

        foreach (var item in order.Items)
        {
            var weapon = weapons.Single(w => w.Id == item.WeaponId);
            if (!weapon.IsAvailable || weapon.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Not enough stock for {weapon.Name}.");
            }

            weapon.StockQuantity -= item.Quantity;
            if (weapon.StockQuantity <= 0)
            {
                weapon.IsAvailable = false;
            }
        }

        order.StockReservedAtUtc = now;
    }

    private static void RecalculateTotalPrice(Order order)
    {
        order.TotalPrice = order.Items.Sum(item => item.UnitPrice * item.Quantity);
    }

    private async Task EnsureUserCanPurchaseAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _applicationUserRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User was not found.");
        }

        if (!user.DateOfBirth.HasValue)
        {
            throw new InvalidOperationException("Set your date of birth before adding items to cart.");
        }

        if (!IsAdult(user.DateOfBirth.Value))
        {
            throw new InvalidOperationException("You must be at least 18 years old to buy weapons.");
        }

        var hasRequiredDocument = !string.IsNullOrWhiteSpace(user.IdCardFileName)
            || !string.IsNullOrWhiteSpace(user.DriverLicenseFileName);

        if (!hasRequiredDocument)
        {
            throw new InvalidOperationException("Upload either ID card or driver license before adding items to cart.");
        }
    }

    private static bool IsAdult(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;

        if (today < dateOfBirth.AddYears(age))
        {
            age--;
        }

        return age >= 18;
    }
}
