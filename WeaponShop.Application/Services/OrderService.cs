using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;
using WeaponShop.Domain.Identity;

namespace WeaponShop.Application.Services;

public class OrderService : IOrderService
{
    private const string DeliveryPickup = "pickup";
    private const string DeliveryShipping = "shipping";
    private const string PaymentBankTransfer = "bank-transfer";
    private const string PaymentCashOnDelivery = "cash-on-delivery";
    private const string PaymentCashOnPickup = "cash-on-pickup";

    private readonly IOrderRepository _orderRepository;
    private readonly IWeaponRepository _weaponRepository;
    private readonly IAccessoryRepository _accessoryRepository;
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailSender _emailSender;

    public OrderService(
        IOrderRepository orderRepository,
        IWeaponRepository weaponRepository,
        IAccessoryRepository accessoryRepository,
        IApplicationUserRepository applicationUserRepository,
        INotificationRepository notificationRepository,
        IEmailSender emailSender)
    {
        _orderRepository = orderRepository;
        _weaponRepository = weaponRepository;
        _accessoryRepository = accessoryRepository;
        _applicationUserRepository = applicationUserRepository;
        _notificationRepository = notificationRepository;
        _emailSender = emailSender;
    }

    public async Task<Order> CreateOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
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
            throw new ArgumentException("ID uživatele, který provádí akci, je povinné.", nameof(actorUserId));
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
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
        }

        return await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken);
    }

    public async Task<List<Order>> GetSubmittedOrdersAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
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
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
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
            throw new ArgumentOutOfRangeException(nameof(quantity), "Množství musí být větší než nula.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Objednávka s ID {orderId} nebyla nalezena.");
        }

        return await AddWeaponItemInternalAsync(order, weaponId, quantity, cancellationToken);
    }

    public async Task<Order> AddItemToCurrentOrderAsync(
        string userId,
        int weaponId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
        }

        await EnsureUserCanPurchaseRestrictedItemsAsync(userId, cancellationToken);

        var currentOrder = await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken)
            ?? await CreateOrderAsync(userId, cancellationToken);

        return await AddWeaponItemInternalAsync(currentOrder, weaponId, quantity, cancellationToken);
    }

    public async Task<Order> AddAccessoryItemToCurrentOrderAsync(
        string userId,
        int accessoryId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
        }

        await EnsureUserExistsAsync(userId, cancellationToken);

        var currentOrder = await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken)
            ?? await CreateOrderAsync(userId, cancellationToken);

        return await AddAccessoryItemInternalAsync(currentOrder, accessoryId, quantity, cancellationToken);
    }

    public async Task<Order> RemoveItemFromCurrentOrderAsync(
        string userId,
        int weaponId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
        }

        var currentOrder = await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken);
        if (currentOrder is null)
        {
            throw new InvalidOperationException("Aktuální objednávka nebyla nalezena.");
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

    public async Task<Order> RemoveAccessoryItemFromCurrentOrderAsync(
        string userId,
        int accessoryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
        }

        var currentOrder = await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken);
        if (currentOrder is null)
        {
            throw new InvalidOperationException("Aktuální objednávka nebyla nalezena.");
        }

        var existingItem = currentOrder.Items.SingleOrDefault(item => item.AccessoryId == accessoryId);
        if (existingItem is null)
        {
            return currentOrder;
        }

        currentOrder.Items.Remove(existingItem);
        RecalculateTotalPrice(currentOrder);
        await _orderRepository.SaveChangesAsync(cancellationToken);
        return currentOrder;
    }

    public async Task<Order> CheckoutCurrentOrderAsync(
        string userId,
        CheckoutDetails checkoutDetails,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
        }

        var user = await EnsureUserExistsAsync(userId, cancellationToken);
        var currentOrder = await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken);
        if (currentOrder is null)
        {
            throw new InvalidOperationException("Aktuální objednávka nebyla nalezena.");
        }

        if (currentOrder.Items.Count == 0)
        {
            throw new InvalidOperationException("Prázdnou objednávku nelze odeslat.");
        }

        var containsRestrictedItems = OrderContainsRestrictedItems(currentOrder);
        if (containsRestrictedItems)
        {
            await EnsureUserCanPurchaseRestrictedItemsAsync(userId, cancellationToken);
        }

        ValidateCheckoutDetails(checkoutDetails, containsRestrictedItems);
        ApplyCheckoutDetails(currentOrder, checkoutDetails, user);

        var now = DateTime.UtcNow;
        await ReserveStockIfNeededAsync(currentOrder, now, cancellationToken);

        var fromStatus = currentOrder.Status;
        currentOrder.Status = containsRestrictedItems ? OrderStatus.AwaitingApproval : OrderStatus.Approved;
        if (!containsRestrictedItems)
        {
            currentOrder.ApprovedAtUtc = now;
        }

        await _orderRepository.AddAuditAsync(new OrderAudit
        {
            OrderId = currentOrder.Id,
            FromStatus = fromStatus,
            ToStatus = currentOrder.Status,
            Action = containsRestrictedItems ? "CheckoutSubmitted" : "PublicOrderAutoApproved",
            ActorUserId = userId,
            ActorName = $"{user.FirstName} {user.LastName}".Trim(),
            ActorRole = "Customer",
            OccurredAtUtc = now,
            Notes = currentOrder.CustomerNote
        }, cancellationToken);

        var (title, message) = BuildCheckoutNotification(currentOrder, containsRestrictedItems, now);
        await _notificationRepository.AddAsync(new Notification
        {
            UserId = currentOrder.UserId,
            OrderId = currentOrder.Id,
            Title = title,
            Message = message,
            CreatedAtUtc = now,
            IsRead = false
        }, cancellationToken);

        await _orderRepository.SaveChangesAsync(cancellationToken);
        await SendCustomerEmailAsync(currentOrder, title, message, cancellationToken);
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
            throw new KeyNotFoundException($"Objednávka s ID {orderId} nebyla nalezena.");
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
            throw new KeyNotFoundException($"Objednávka s ID {orderId} nebyla nalezena.");
        }

        var now = DateTime.UtcNow;
        var fromStatus = order.Status;
        var containsRestrictedItems = OrderContainsRestrictedItems(order);

        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new InvalidOperationException("Pro auditované změny stavu je vyžadováno ID uživatele.");
        }

        switch (status)
        {
            case OrderStatus.Approved:
                if (order.Status != OrderStatus.AwaitingApproval)
                {
                    throw new InvalidOperationException("Schválit lze pouze objednávku čekající na schválení.");
                }

                order.ApprovedAtUtc = now;
                order.Status = OrderStatus.Approved;
                break;
            case OrderStatus.Rejected:
                if (order.Status is OrderStatus.Rejected or OrderStatus.Completed or OrderStatus.Shipped)
                {
                    throw new InvalidOperationException("Tuto objednávku v aktuální fázi nelze zamítnout.");
                }

                await ReleaseStockIfReservedAsync(order, cancellationToken);
                order.RejectedAtUtc = now;
                order.Status = OrderStatus.Rejected;
                break;
            case OrderStatus.AwaitingGunsmith:
                if (order.Status != OrderStatus.Approved)
                {
                    throw new InvalidOperationException("Objednávka musí být nejprve schválena administrátorem, než ji sklad předá zbrojíři.");
                }

                if (!containsRestrictedItems)
                {
                    throw new InvalidOperationException("Veřejné objednávky se zbrojíři nepředávají.");
                }

                order.WarehouseCheckedAtUtc = now;
                order.Status = OrderStatus.AwaitingGunsmith;
                break;
            case OrderStatus.AwaitingDispatch:
                if (order.Status != OrderStatus.AwaitingGunsmith)
                {
                    throw new InvalidOperationException("Objednávka musí být nejprve u zbrojíře, aby se mohla vrátit zpět na sklad.");
                }

                order.GunsmithCheckedAtUtc = now;
                order.Status = OrderStatus.AwaitingDispatch;
                break;
            case OrderStatus.Shipped:
                if (order.Status != OrderStatus.AwaitingDispatch && !(order.Status == OrderStatus.Approved && !containsRestrictedItems))
                {
                    throw new InvalidOperationException("Objednávka musí být připravena skladem, než může být odeslána.");
                }

                await ReserveStockIfNeededAsync(order, now, cancellationToken);
                order.WarehousePreparedAtUtc ??= now;
                order.ShippedAtUtc = now;
                order.Status = OrderStatus.Shipped;
                break;
            case OrderStatus.ReadyForPickup:
                if (order.Status != OrderStatus.AwaitingDispatch && !(order.Status == OrderStatus.Approved && !containsRestrictedItems))
                {
                    throw new InvalidOperationException("Objednávka musí být připravena skladem, než bude nachystána k vyzvednutí.");
                }

                await ReserveStockIfNeededAsync(order, now, cancellationToken);
                order.WarehousePreparedAtUtc ??= now;
                order.ReadyForPickupAtUtc = now;
                order.Status = OrderStatus.ReadyForPickup;
                break;
            case OrderStatus.Completed:
                if (order.Status != OrderStatus.ReadyForPickup)
                {
                    throw new InvalidOperationException("Objednávka musí být připravena k vyzvednutí, než bude předána zákazníkovi.");
                }

                order.PickupHandedOverAtUtc = now;
                order.Status = OrderStatus.Completed;
                break;
            default:
                order.Status = status;
                break;
        }

        var action = ResolveAuditAction(status, actorRole, containsRestrictedItems);
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
            var (title, message) = BuildCustomerNotification(order, now, notes, containsRestrictedItems);
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
            var (title, message) = BuildCustomerNotification(order, now, null, containsRestrictedItems);
            await SendCustomerEmailAsync(order, title, message, cancellationToken);
        }

        return order;
    }

    public async Task DeleteOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException($"Objednávka s ID {orderId} nebyla nalezena.");
        }

        if (order.Status == OrderStatus.Created)
        {
            throw new InvalidOperationException("Aktuální košík nelze z historie smazat.");
        }

        _orderRepository.Remove(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Order> AddWeaponItemInternalAsync(
        Order order,
        int weaponId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Množství musí být větší než nula.");
        }

        var weapon = await _weaponRepository.GetByIdAsync(weaponId, cancellationToken);
        if (weapon is null)
        {
            throw new KeyNotFoundException($"Zbraň s ID {weaponId} nebyla nalezena.");
        }

        if (!weapon.IsAvailable || weapon.StockQuantity <= 0)
        {
            throw new InvalidOperationException("Vybraná zbraň je momentálně nedostupná.");
        }

        var existingItem = order.Items.SingleOrDefault(item => item.WeaponId == weaponId);
        if (existingItem is null)
        {
            if (quantity > weapon.StockQuantity)
            {
                throw new InvalidOperationException("Požadované množství přesahuje dostupný sklad.");
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
                throw new InvalidOperationException("Požadované množství přesahuje dostupný sklad.");
            }

            existingItem.Quantity = newQuantity;
            existingItem.UnitPrice = weapon.Price;
        }

        RecalculateTotalPrice(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);
        return order;
    }

    private async Task<Order> AddAccessoryItemInternalAsync(
        Order order,
        int accessoryId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Množství musí být větší než nula.");
        }

        var accessory = await _accessoryRepository.GetByIdAsync(accessoryId, cancellationToken);
        if (accessory is null)
        {
            throw new KeyNotFoundException($"Doplněk s ID {accessoryId} nebyl nalezen.");
        }

        if (!accessory.IsAvailable || accessory.StockQuantity <= 0)
        {
            throw new InvalidOperationException("Vybraný doplněk je momentálně nedostupný.");
        }

        var existingItem = order.Items.SingleOrDefault(item => item.AccessoryId == accessoryId);
        if (existingItem is null)
        {
            if (quantity > accessory.StockQuantity)
            {
                throw new InvalidOperationException("Požadované množství přesahuje dostupný sklad.");
            }

            order.Items.Add(new OrderItem
            {
                AccessoryId = accessoryId,
                Quantity = quantity,
                UnitPrice = accessory.Price
            });
        }
        else
        {
            var newQuantity = existingItem.Quantity + quantity;
            if (newQuantity > accessory.StockQuantity)
            {
                throw new InvalidOperationException("Požadované množství přesahuje dostupný sklad.");
            }

            existingItem.Quantity = newQuantity;
            existingItem.UnitPrice = accessory.Price;
        }

        RecalculateTotalPrice(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);
        return order;
    }

    private static string ResolveAuditAction(OrderStatus status, string? actorRole, bool containsRestrictedItems)
    {
        return status switch
        {
            OrderStatus.Approved => containsRestrictedItems ? "AdminApproved" : "PublicOrderConfirmed",
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

    private static (string Title, string Message) BuildCheckoutNotification(Order order, bool containsRestrictedItems, DateTime now)
    {
        return containsRestrictedItems
            ? ($"Objednávka č. {order.Id} čeká na schválení",
                $"Objednávka č. {order.Id} byla přijata dne {now:yyyy-MM-dd HH:mm} UTC a čeká na ověření kontrolovaného zboží.")
            : ($"Objednávka č. {order.Id} byla přijata",
                $"Objednávka č. {order.Id} byla přijata dne {now:yyyy-MM-dd HH:mm} UTC a předána skladu k vyřízení.");
    }

    private static (string Title, string Message) BuildCustomerNotification(
        Order order,
        DateTime now,
        string? notes,
        bool containsRestrictedItems)
    {
        var reason = string.IsNullOrWhiteSpace(notes) ? "" : $" Důvod: {notes}";
        return order.Status switch
        {
            OrderStatus.Approved when containsRestrictedItems => ($"Objednávka č. {order.Id} byla schválena",
                $"Administrátor potvrdil doklady a objednávka č. {order.Id} byla předána skladu ({now:yyyy-MM-dd HH:mm} UTC)."),
            OrderStatus.Approved => ($"Objednávka č. {order.Id} byla potvrzena",
                $"Sklad může zahájit zpracování objednávky č. {order.Id} ({now:yyyy-MM-dd HH:mm} UTC)."),
            OrderStatus.AwaitingGunsmith => ($"Objednávka č. {order.Id} míří ke zbrojíři",
                $"Sklad potvrdil kontrolu zásob. Objednávka č. {order.Id} byla předána zbrojíři ({now:yyyy-MM-dd HH:mm} UTC)."),
            OrderStatus.AwaitingDispatch => ($"Objednávka č. {order.Id} se vrací na sklad",
                $"Zbrojíř dokončil kontrolu. Objednávka č. {order.Id} byla vrácena zpět skladu ({now:yyyy-MM-dd HH:mm} UTC)."),
            OrderStatus.Rejected => ($"Objednávka č. {order.Id} byla zamítnuta",
                $"Objednávka č. {order.Id} byla zamítnuta ({now:yyyy-MM-dd HH:mm} UTC).{reason}"),
            OrderStatus.Shipped => ($"Objednávka č. {order.Id} byla odeslána",
                $"Vaše objednávka č. {order.Id} byla odeslána dne {now:yyyy-MM-dd HH:mm} UTC."),
            OrderStatus.ReadyForPickup => ($"Objednávka č. {order.Id} je připravena k vyzvednutí",
                $"Vaše objednávka č. {order.Id} je připravena k vyzvednutí od {now:yyyy-MM-dd HH:mm} UTC."),
            OrderStatus.Completed => ($"Objednávka č. {order.Id} byla převzata",
                $"Osobní odběr byl potvrzen dne {now:yyyy-MM-dd HH:mm} UTC."),
            _ => ("Objednávka byla aktualizována", $"Vaše objednávka č. {order.Id} byla aktualizována.")
        };
    }

    private async Task SendCustomerEmailAsync(
        Order order,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var email = !string.IsNullOrWhiteSpace(order.ContactEmail)
            ? order.ContactEmail
            : order.User?.Email;

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        await _emailSender.SendAsync(email, title, message, cancellationToken);
    }

    private async Task ReserveStockIfNeededAsync(Order order, DateTime now, CancellationToken cancellationToken)
    {
        if (order.StockReservedAtUtc.HasValue)
        {
            return;
        }

        var weaponIds = order.Items
            .Where(item => item.WeaponId.HasValue)
            .Select(item => item.WeaponId!.Value)
            .Distinct()
            .ToList();
        var accessoryIds = order.Items
            .Where(item => item.AccessoryId.HasValue)
            .Select(item => item.AccessoryId!.Value)
            .Distinct()
            .ToList();

        var weapons = weaponIds.Count == 0
            ? new List<Weapon>()
            : await _weaponRepository.GetByIdsForUpdateAsync(weaponIds, cancellationToken);
        var accessories = accessoryIds.Count == 0
            ? new List<Accessory>()
            : await _accessoryRepository.GetByIdsForUpdateAsync(accessoryIds, cancellationToken);

        if (weapons.Count != weaponIds.Count)
        {
            throw new InvalidOperationException("Nelze rezervovat sklad: zbraň nebyla nalezena.");
        }

        if (accessories.Count != accessoryIds.Count)
        {
            throw new InvalidOperationException("Nelze rezervovat sklad: doplněk nebyl nalezen.");
        }

        foreach (var item in order.Items)
        {
            if (item.WeaponId.HasValue)
            {
                var weapon = weapons.Single(w => w.Id == item.WeaponId.Value);
                if (!weapon.IsAvailable || weapon.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Pro {weapon.Name} není dostatek skladových zásob.");
                }

                weapon.StockQuantity -= item.Quantity;
                if (weapon.StockQuantity <= 0)
                {
                    weapon.IsAvailable = false;
                }

                continue;
            }

            if (item.AccessoryId.HasValue)
            {
                var accessory = accessories.Single(a => a.Id == item.AccessoryId.Value);
                if (!accessory.IsAvailable || accessory.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Pro {accessory.Name} není dostatek skladových zásob.");
                }

                accessory.StockQuantity -= item.Quantity;
                if (accessory.StockQuantity <= 0)
                {
                    accessory.IsAvailable = false;
                }
            }
        }

        order.StockReservedAtUtc = now;
    }

    private async Task ReleaseStockIfReservedAsync(Order order, CancellationToken cancellationToken)
    {
        if (!order.StockReservedAtUtc.HasValue)
        {
            return;
        }

        var weaponIds = order.Items
            .Where(item => item.WeaponId.HasValue)
            .Select(item => item.WeaponId!.Value)
            .Distinct()
            .ToList();
        var accessoryIds = order.Items
            .Where(item => item.AccessoryId.HasValue)
            .Select(item => item.AccessoryId!.Value)
            .Distinct()
            .ToList();

        var weapons = weaponIds.Count == 0
            ? new List<Weapon>()
            : await _weaponRepository.GetByIdsForUpdateAsync(weaponIds, cancellationToken);
        var accessories = accessoryIds.Count == 0
            ? new List<Accessory>()
            : await _accessoryRepository.GetByIdsForUpdateAsync(accessoryIds, cancellationToken);

        foreach (var item in order.Items)
        {
            if (item.WeaponId.HasValue)
            {
                var weapon = weapons.Single(w => w.Id == item.WeaponId.Value);
                weapon.StockQuantity += item.Quantity;
                weapon.IsAvailable = true;
                continue;
            }

            if (item.AccessoryId.HasValue)
            {
                var accessory = accessories.Single(a => a.Id == item.AccessoryId.Value);
                accessory.StockQuantity += item.Quantity;
                accessory.IsAvailable = true;
            }
        }

        order.StockReservedAtUtc = null;
    }

    private static void RecalculateTotalPrice(Order order)
    {
        order.TotalPrice = order.Items.Sum(item => item.UnitPrice * item.Quantity);
    }

    private async Task<ApplicationUser> EnsureUserExistsAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _applicationUserRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("Uživatel nebyl nalezen.");
        }

        return user;
    }

    private async Task EnsureUserCanPurchaseRestrictedItemsAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await EnsureUserExistsAsync(userId, cancellationToken);

        if (!user.DateOfBirth.HasValue)
        {
            throw new InvalidOperationException("Než přidáte zboží do košíku, doplňte datum narození.");
        }

        if (!IsAdult(user.DateOfBirth.Value))
        {
            throw new InvalidOperationException("Pro nákup zbraní musíte být starší 18 let.");
        }

        var hasRequiredDocument = !string.IsNullOrWhiteSpace(user.IdCardFileName)
            || !string.IsNullOrWhiteSpace(user.DriverLicenseFileName);

        if (!hasRequiredDocument)
        {
            throw new InvalidOperationException("Před přidáním zbraní do košíku nahrajte občanský nebo řidičský průkaz.");
        }
    }

    private static bool OrderContainsRestrictedItems(Order order)
    {
        return order.Items.Any(item => item.IsWeapon);
    }

    private static void ValidateCheckoutDetails(CheckoutDetails checkoutDetails, bool containsRestrictedItems)
    {
        if (string.IsNullOrWhiteSpace(checkoutDetails.ContactEmail) || !checkoutDetails.ContactEmail.Contains('@'))
        {
            throw new InvalidOperationException("Vyplňte platný kontaktní e-mail.");
        }

        if (string.IsNullOrWhiteSpace(checkoutDetails.ContactPhone))
        {
            throw new InvalidOperationException("Vyplňte kontaktní telefon.");
        }

        var deliveryMethod = Normalize(checkoutDetails.DeliveryMethod);
        if (deliveryMethod is not (DeliveryPickup or DeliveryShipping))
        {
            throw new InvalidOperationException("Vyberte způsob doručení.");
        }

        var paymentMethod = Normalize(checkoutDetails.PaymentMethod);
        if (paymentMethod is not (PaymentBankTransfer or PaymentCashOnDelivery or PaymentCashOnPickup))
        {
            throw new InvalidOperationException("Vyberte způsob platby.");
        }

        if (containsRestrictedItems && deliveryMethod != DeliveryPickup)
        {
            throw new InvalidOperationException("Objednávky se zbraněmi lze dokončit pouze s osobním odběrem.");
        }

        if (deliveryMethod == DeliveryPickup && paymentMethod == PaymentCashOnDelivery)
        {
            throw new InvalidOperationException("Dobírka není dostupná pro osobní odběr.");
        }

        if (deliveryMethod == DeliveryShipping && paymentMethod == PaymentCashOnPickup)
        {
            throw new InvalidOperationException("Platba při převzetí na prodejně není dostupná pro doručení.");
        }

        if (string.IsNullOrWhiteSpace(checkoutDetails.ShippingName))
        {
            throw new InvalidOperationException("Vyplňte jméno příjemce.");
        }

        if (deliveryMethod == DeliveryShipping)
        {
            if (string.IsNullOrWhiteSpace(checkoutDetails.ShippingStreet)
                || string.IsNullOrWhiteSpace(checkoutDetails.ShippingCity)
                || string.IsNullOrWhiteSpace(checkoutDetails.ShippingPostalCode))
            {
                throw new InvalidOperationException("Pro doručení na adresu vyplňte celou dodací adresu.");
            }
        }

        if (string.IsNullOrWhiteSpace(checkoutDetails.BillingName)
            || string.IsNullOrWhiteSpace(checkoutDetails.BillingStreet)
            || string.IsNullOrWhiteSpace(checkoutDetails.BillingCity)
            || string.IsNullOrWhiteSpace(checkoutDetails.BillingPostalCode))
        {
            throw new InvalidOperationException("Vyplňte fakturační údaje.");
        }
    }

    private static void ApplyCheckoutDetails(Order order, CheckoutDetails checkoutDetails, ApplicationUser user)
    {
        order.ContactEmail = checkoutDetails.ContactEmail.Trim();
        order.ContactPhone = checkoutDetails.ContactPhone.Trim();
        order.DeliveryMethod = Normalize(checkoutDetails.DeliveryMethod);
        order.PaymentMethod = Normalize(checkoutDetails.PaymentMethod);
        order.ShippingName = checkoutDetails.ShippingName.Trim();
        order.ShippingStreet = checkoutDetails.ShippingStreet.Trim();
        order.ShippingCity = checkoutDetails.ShippingCity.Trim();
        order.ShippingPostalCode = checkoutDetails.ShippingPostalCode.Trim();
        order.BillingName = checkoutDetails.BillingName.Trim();
        order.BillingStreet = checkoutDetails.BillingStreet.Trim();
        order.BillingCity = checkoutDetails.BillingCity.Trim();
        order.BillingPostalCode = checkoutDetails.BillingPostalCode.Trim();
        order.CustomerNote = checkoutDetails.CustomerNote.Trim();

        if (string.IsNullOrWhiteSpace(order.ContactEmail))
        {
            order.ContactEmail = user.Email ?? string.Empty;
        }

        if (order.DeliveryMethod == DeliveryPickup && string.IsNullOrWhiteSpace(order.ShippingStreet))
        {
            order.ShippingStreet = "Osobní odběr";
        }

        if (order.DeliveryMethod == DeliveryPickup && string.IsNullOrWhiteSpace(order.ShippingCity))
        {
            order.ShippingCity = "Výdejní místo";
        }

        if (order.DeliveryMethod == DeliveryPickup && string.IsNullOrWhiteSpace(order.ShippingPostalCode))
        {
            order.ShippingPostalCode = "000 00";
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

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
