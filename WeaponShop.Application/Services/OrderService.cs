using System.Globalization;
using System.Net;
using System.Text;
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
    private readonly IInvoiceDocumentService _invoiceDocumentService;

    public OrderService(
        IOrderRepository orderRepository,
        IWeaponRepository weaponRepository,
        IAccessoryRepository accessoryRepository,
        IApplicationUserRepository applicationUserRepository,
        INotificationRepository notificationRepository,
        IEmailSender emailSender,
        IInvoiceDocumentService invoiceDocumentService)
    {
        _orderRepository = orderRepository;
        _weaponRepository = weaponRepository;
        _accessoryRepository = accessoryRepository;
        _applicationUserRepository = applicationUserRepository;
        _notificationRepository = notificationRepository;
        _emailSender = emailSender;
        _invoiceDocumentService = invoiceDocumentService;
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

    public async Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is not null)
        {
            await PopulateAuditActorNamesAsync(order.Audits, cancellationToken);
        }

        return order;
    }

    public async Task<List<OrderAudit>> GetAuditsByActorAsync(string actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new ArgumentException("ID uživatele, který provádí akci, je povinné.", nameof(actorUserId));
        }

        var audits = await _orderRepository.GetAuditsByActorAsync(actorUserId, cancellationToken);
        await PopulateAuditActorNamesAsync(audits, cancellationToken);
        return audits;
    }

    public async Task<List<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        await PopulateAuditActorNamesAsync(orders.SelectMany(order => order.Audits), cancellationToken);
        return orders;
    }

    public async Task<Order?> GetCurrentOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
        }

        var order = await _orderRepository.GetCurrentByUserIdAsync(userId, cancellationToken);
        if (order is not null)
        {
            await PopulateAuditActorNamesAsync(order.Audits, cancellationToken);
        }

        return order;
    }

    public async Task<List<Order>> GetSubmittedOrdersAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("ID uživatele je povinné.", nameof(userId));
        }

        var allUserOrders = await _orderRepository.GetByUserIdAsync(userId, cancellationToken);
        await PopulateAuditActorNamesAsync(allUserOrders.SelectMany(order => order.Audits), cancellationToken);
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

        var orders = await _orderRepository.GetByUserIdAsync(userId, cancellationToken);
        await PopulateAuditActorNamesAsync(orders.SelectMany(order => order.Audits), cancellationToken);
        return orders;
    }

    public async Task<List<Order>> GetAwaitingApprovalOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAwaitingApprovalAsync(cancellationToken);
        await PopulateAuditActorNamesAsync(orders.SelectMany(order => order.Audits), cancellationToken);
        return orders;
    }

    public async Task<List<Order>> GetWarehouseOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByStatusesAsync(
            new[] { OrderStatus.Approved, OrderStatus.AwaitingDispatch, OrderStatus.ReadyForPickup },
            cancellationToken);
        await PopulateAuditActorNamesAsync(orders.SelectMany(order => order.Audits), cancellationToken);
        return orders;
    }

    public async Task<List<Order>> GetGunsmithOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByStatusesAsync(
            new[] { OrderStatus.AwaitingGunsmith },
            cancellationToken);
        await PopulateAuditActorNamesAsync(orders.SelectMany(order => order.Audits), cancellationToken);
        return orders;
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
        currentOrder.OrderNumber ??= GeneratePublicOrderNumber(now);
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
            Action = containsRestrictedItems
                ? "Objednávka odeslána ke schválení"
                : "Veřejná objednávka byla automaticky potvrzena",
            ActorUserId = userId,
            ActorName = $"{user.FirstName} {user.LastName}".Trim(),
            ActorRole = "Zákazník",
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
        await SendCheckoutEmailsAsync(currentOrder, containsRestrictedItems, now, cancellationToken);
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

        order.OrderNumber ??= GeneratePublicOrderNumber(now);

        var resolvedActorName = await ResolveActorNameAsync(actorUserId, actorName, cancellationToken);
        var action = ResolveAuditAction(status, actorRole, containsRestrictedItems);
        await _orderRepository.AddAuditAsync(new OrderAudit
        {
            OrderId = order.Id,
            FromStatus = fromStatus,
            ToStatus = order.Status,
            Action = action,
            ActorUserId = actorUserId,
            ActorName = resolvedActorName,
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

        if (order.Status is OrderStatus.Approved
            or OrderStatus.AwaitingGunsmith
            or OrderStatus.AwaitingDispatch
            or OrderStatus.Rejected
            or OrderStatus.Shipped
            or OrderStatus.ReadyForPickup
            or OrderStatus.Completed)
        {
            await SendStatusEmailsAsync(order, now, notes, containsRestrictedItems, cancellationToken);
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
            OrderStatus.Approved => containsRestrictedItems
                ? "Administrátor schválil objednávku"
                : "Veřejná objednávka byla potvrzena",
            OrderStatus.Rejected => actorRole switch
            {
                "Skladník" => "Sklad zamítl objednávku",
                "Zbrojíř" => "Zbrojíř zamítl objednávku",
                _ => "Administrátor zamítl objednávku"
            },
            OrderStatus.AwaitingGunsmith => "Sklad předal objednávku zbrojíři",
            OrderStatus.AwaitingDispatch => "Zbrojíř vrátil objednávku na sklad",
            OrderStatus.Shipped => "Objednávka byla odeslána",
            OrderStatus.ReadyForPickup => "Objednávka je připravena k vyzvednutí",
            OrderStatus.Completed => "Objednávka byla předána zákazníkovi",
            _ => $"Změna stavu: {status}"
        };
    }

    private async Task PopulateAuditActorNamesAsync(IEnumerable<OrderAudit> audits, CancellationToken cancellationToken)
    {
        var auditList = audits
            .Where(audit => !string.IsNullOrWhiteSpace(audit.ActorUserId))
            .ToList();

        if (auditList.Count == 0)
        {
            return;
        }

        var users = await _applicationUserRepository.GetByIdsAsync(
            auditList.Select(audit => audit.ActorUserId),
            cancellationToken);

        var displayNamesByUserId = users.ToDictionary(
            user => user.Id,
            GetUserDisplayName,
            StringComparer.Ordinal);

        foreach (var audit in auditList)
        {
            if (displayNamesByUserId.TryGetValue(audit.ActorUserId, out var displayName)
                && !string.IsNullOrWhiteSpace(displayName))
            {
                audit.ActorName = displayName;
            }
        }
    }

    private async Task<string> ResolveActorNameAsync(
        string actorUserId,
        string? fallbackActorName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(actorUserId))
        {
            var user = await _applicationUserRepository.GetByIdAsync(actorUserId, cancellationToken);
            var displayName = GetUserDisplayName(user);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }
        }

        return fallbackActorName?.Trim() ?? string.Empty;
    }

    private static string GetUserDisplayName(ApplicationUser? user)
    {
        if (user is null)
        {
            return string.Empty;
        }

        return $"{user.FirstName} {user.LastName}".Trim();
    }

    private static string GeneratePublicOrderNumber(DateTime now)
    {
        return $"WS-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    }

    private static (string Title, string Message) BuildCheckoutNotification(Order order, bool containsRestrictedItems, DateTime now)
    {
        var localTime = now.ToLocalTime();
        var orderNumber = order.GetPublicOrderNumber();
        return containsRestrictedItems
            ? ($"Objednávka č. {orderNumber} čeká na schválení",
                $"Objednávka č. {orderNumber} byla přijata dne {localTime:dd. MM. yyyy HH:mm} a čeká na ověření kontrolovaného zboží.")
            : ($"Objednávka č. {orderNumber} byla přijata",
                $"Objednávka č. {orderNumber} byla přijata dne {localTime:dd. MM. yyyy HH:mm} a předána skladu k vyřízení.");
    }

    private static (string Title, string Message) BuildCustomerNotification(
        Order order,
        DateTime now,
        string? notes,
        bool containsRestrictedItems)
    {
        var localTime = now.ToLocalTime();
        var reason = string.IsNullOrWhiteSpace(notes) ? "" : $" Důvod: {notes}";
        var orderNumber = order.GetPublicOrderNumber();
        return order.Status switch
        {
            OrderStatus.Approved when containsRestrictedItems => ($"Objednávka č. {orderNumber} byla schválena",
                $"Administrátor potvrdil doklady a objednávka č. {orderNumber} byla předána skladu ({localTime:dd. MM. yyyy HH:mm})."),
            OrderStatus.Approved => ($"Objednávka č. {orderNumber} byla potvrzena",
                $"Sklad může zahájit zpracování objednávky č. {orderNumber} ({localTime:dd. MM. yyyy HH:mm})."),
            OrderStatus.AwaitingGunsmith => ($"Objednávka č. {orderNumber} míří ke zbrojíři",
                $"Sklad potvrdil kontrolu zásob. Objednávka č. {orderNumber} byla předána zbrojíři ({localTime:dd. MM. yyyy HH:mm})."),
            OrderStatus.AwaitingDispatch => ($"Objednávka č. {orderNumber} se vrací na sklad",
                $"Zbrojíř dokončil kontrolu. Objednávka č. {orderNumber} byla vrácena zpět skladu ({localTime:dd. MM. yyyy HH:mm})."),
            OrderStatus.Rejected when IsDocumentCompletionRequest(notes) => ($"Je potřeba doplnit doklady k objednávce č. {orderNumber}",
                $"Objednávku č. {orderNumber} nelze zatím dokončit. Doplňte prosím požadované doklady a odešlete je znovu.{reason}"),
            OrderStatus.Rejected => ($"Objednávka č. {orderNumber} byla zamítnuta",
                $"Objednávka č. {orderNumber} byla zamítnuta ({localTime:dd. MM. yyyy HH:mm}).{reason}"),
            OrderStatus.Shipped => ($"Objednávka č. {orderNumber} byla odeslána",
                $"Vaše objednávka č. {orderNumber} byla odeslána dne {localTime:dd. MM. yyyy HH:mm}."),
            OrderStatus.ReadyForPickup => ($"Objednávka č. {orderNumber} je připravena k vyzvednutí",
                $"Vaše objednávka č. {orderNumber} je připravena k vyzvednutí od {localTime:dd. MM. yyyy HH:mm}."),
            OrderStatus.Completed => ($"Objednávka č. {orderNumber} byla převzata",
                $"Osobní odběr byl potvrzen dne {localTime:dd. MM. yyyy HH:mm}."),
            _ => ("Objednávka byla aktualizována", $"Vaše objednávka č. {orderNumber} byla aktualizována.")
        };
    }

    private async Task SendCheckoutEmailsAsync(
        Order order,
        bool containsRestrictedItems,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var (confirmationTitle, confirmationMessage) = BuildOrderReceivedEmail(order, now);
        await SendCustomerEmailAsync(
            order,
            confirmationTitle,
            confirmationMessage,
            attachInvoice: false,
            cancellationToken: cancellationToken);

        if (!containsRestrictedItems)
        {
            return;
        }

        var (verificationTitle, verificationMessage) = BuildAwaitingVerificationEmail(order, now);
        await SendCustomerEmailAsync(
            order,
            verificationTitle,
            verificationMessage,
            attachInvoice: false,
            cancellationToken: cancellationToken);
    }

    private async Task SendStatusEmailsAsync(
        Order order,
        DateTime now,
        string? notes,
        bool containsRestrictedItems,
        CancellationToken cancellationToken)
    {
        var statusEmail = BuildStatusEmail(order, now, notes, containsRestrictedItems);
        if (statusEmail.HasValue)
        {
            await SendCustomerEmailAsync(
                order,
                statusEmail.Value.Title,
                statusEmail.Value.Message,
                attachInvoice: false,
                cancellationToken: cancellationToken);
        }

        var invoiceEmail = BuildInvoiceEmail(order, now);
        if (invoiceEmail.HasValue)
        {
            await SendCustomerEmailAsync(
                order,
                invoiceEmail.Value.Title,
                invoiceEmail.Value.Message,
                attachInvoice: true,
                cancellationToken: cancellationToken);
        }
    }

    private static (string Title, string Message) BuildOrderReceivedEmail(Order order, DateTime now)
    {
        var localTime = now.ToLocalTime();
        var orderNumber = order.GetPublicOrderNumber();
        return ($"Potvrzení přijetí objednávky č. {orderNumber}",
            $"Potvrzujeme přijetí objednávky č. {orderNumber} ze dne {localTime:dd. MM. yyyy HH:mm}. Rekapitulaci objednávky najdete níže v tomto e-mailu.");
    }

    private static (string Title, string Message) BuildAwaitingVerificationEmail(Order order, DateTime now)
    {
        var localTime = now.ToLocalTime();
        var orderNumber = order.GetPublicOrderNumber();
        return ($"Objednávka č. {orderNumber} čeká na ověření",
            $"Objednávka č. {orderNumber} obsahuje regulované zboží a od {localTime:dd. MM. yyyy HH:mm} čeká na kontrolu věku, dokladů a schválení objednávky.");
    }

    private static (string Title, string Message)? BuildStatusEmail(
        Order order,
        DateTime now,
        string? notes,
        bool containsRestrictedItems)
    {
        var localTime = now.ToLocalTime();
        var reason = string.IsNullOrWhiteSpace(notes) ? string.Empty : $" Důvod: {notes}";
        var orderNumber = order.GetPublicOrderNumber();

        return order.Status switch
        {
            OrderStatus.Approved => ($"Objednávka č. {orderNumber} byla schválena",
                containsRestrictedItems
                    ? $"Objednávka č. {orderNumber} byla dne {localTime:dd. MM. yyyy HH:mm} schválena po kontrole zákaznického profilu a dokladů."
                    : $"Objednávka č. {orderNumber} byla dne {localTime:dd. MM. yyyy HH:mm} schválena a předána k dalšímu zpracování."),
            OrderStatus.Rejected when IsDocumentCompletionRequest(notes) => ($"Je potřeba doplnit doklady k objednávce č. {orderNumber}",
                $"Objednávku č. {orderNumber} zatím nelze schválit. Doplňte prosím požadované doklady v zákaznickém účtu a objednávku následně znovu odešlete.{reason}"),
            OrderStatus.Rejected => ($"Objednávka č. {orderNumber} byla zamítnuta",
                $"Objednávka č. {orderNumber} byla dne {localTime:dd. MM. yyyy HH:mm} zamítnuta.{reason}"),
            OrderStatus.ReadyForPickup => ($"Objednávka č. {orderNumber} je připravena k odběru",
                $"Objednávka č. {orderNumber} je od {localTime:dd. MM. yyyy HH:mm} připravena k osobnímu odběru. Přineste si prosím potřebné doklady a číslo objednávky."),
            OrderStatus.Shipped => ($"Objednávka č. {orderNumber} byla expedována",
                $"Objednávka č. {orderNumber} byla dne {localTime:dd. MM. yyyy HH:mm} předána k doručení na uvedenou adresu."),
            _ => null
        };
    }

    private static (string Title, string Message)? BuildInvoiceEmail(Order order, DateTime now)
    {
        var localTime = now.ToLocalTime();
        var orderNumber = order.GetPublicOrderNumber();

        return order.Status switch
        {
            OrderStatus.ReadyForPickup => ($"Faktura k objednávce č. {orderNumber}",
                $"K objednávce č. {orderNumber}, která je od {localTime:dd. MM. yyyy HH:mm} připravena k odběru, přikládáme fakturu v PDF."),
            OrderStatus.Shipped => ($"Faktura k objednávce č. {orderNumber}",
                $"K objednávce č. {orderNumber}, která byla dne {localTime:dd. MM. yyyy HH:mm} expedována, přikládáme fakturu v PDF."),
            _ => null
        };
    }

    private static bool IsDocumentCompletionRequest(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return false;
        }

        var normalized = notes.ToLowerInvariant();
        return normalized.Contains("doklad")
            || normalized.Contains("občansk")
            || normalized.Contains("obciansk")
            || normalized.Contains("povolen")
            || normalized.Contains("ověřen")
            || normalized.Contains("overen")
            || normalized.Contains("věk")
            || normalized.Contains("vek")
            || normalized.Contains("identit");
    }

    private async Task SendCustomerEmailAsync(
        Order order,
        string title,
        string message,
        bool attachInvoice,
        CancellationToken cancellationToken)
    {
        var email = !string.IsNullOrWhiteSpace(order.ContactEmail)
            ? order.ContactEmail
            : order.User?.Email;

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var invoice = attachInvoice ? _invoiceDocumentService.BuildInvoice(order) : null;
        var body = BuildCustomerEmailBody(order, title, message, invoice?.InvoiceNumber);
        var attachments = invoice is null
            ? null
            : new[]
            {
                new EmailAttachment
                {
                    FileName = invoice.PdfFileName,
                    ContentType = "application/pdf",
                    Content = invoice.PdfContent
                }
            };

        await _emailSender.SendAsync(
            email,
            title,
            body,
            isHtml: true,
            attachments: attachments,
            cancellationToken: cancellationToken);
    }

    private static string BuildCustomerEmailBody(Order order, string title, string message, string? invoiceNumber)
    {
        var culture = new CultureInfo("cs-CZ");
        var orderNumber = WebUtility.HtmlEncode(order.GetPublicOrderNumber());
        var createdAt = WebUtility.HtmlEncode(order.CreatedAt.ToLocalTime().ToString("d. M. yyyy HH:mm", culture));
        var totalPrice = WebUtility.HtmlEncode(order.TotalPrice.ToString("C", culture));
        var encodedTitle = WebUtility.HtmlEncode(title);
        var encodedMessage = WebUtility.HtmlEncode(message);
        var shippingName = WebUtility.HtmlEncode(order.ShippingName);
        var shippingStreet = WebUtility.HtmlEncode(order.ShippingStreet);
        var shippingCity = WebUtility.HtmlEncode($"{order.ShippingPostalCode} {order.ShippingCity}".Trim());
        var billingName = WebUtility.HtmlEncode(order.BillingName);
        var billingStreet = WebUtility.HtmlEncode(order.BillingStreet);
        var billingCity = WebUtility.HtmlEncode($"{order.BillingPostalCode} {order.BillingCity}".Trim());

        var invoiceBlock = string.IsNullOrWhiteSpace(invoiceNumber)
            ? string.Empty
            : $$"""
            <div style="border:1px solid #d6c4a1;background:#fff8e8;padding:16px 18px;border-radius:12px;margin-bottom:24px;">
                <strong style="display:block;margin-bottom:6px;">Faktura byla vygenerována.</strong>
                <span style="font-size:14px;color:#5c4b27;">K objednávce je přiložen elektronický doklad {{WebUtility.HtmlEncode(invoiceNumber)}}.</span>
            </div>
""";

        var rows = string.Join(
            string.Empty,
            order.Items.Select(item =>
                $$"""
                    <tr>
                        <td style="padding:12px;border-bottom:1px solid #ece6d8;">{{WebUtility.HtmlEncode(item.GetDisplayName())}}</td>
                        <td style="padding:12px;border-bottom:1px solid #ece6d8;text-align:right;">{{item.Quantity.ToString(culture)}}</td>
                        <td style="padding:12px;border-bottom:1px solid #ece6d8;text-align:right;">{{WebUtility.HtmlEncode((item.UnitPrice * item.Quantity).ToString("C", culture))}}</td>
                    </tr>
"""));

        return $$"""
<!DOCTYPE html>
<html lang="cs">
<head>
    <meta charset="utf-8">
    <title>Stav objednávky</title>
</head>
<body style="margin:0;padding:0;background:#f4f1ea;font-family:Arial,Helvetica,sans-serif;color:#182230;">
    <div style="max-width:760px;margin:0 auto;padding:24px 16px;">
        <div style="background:#182230;color:#ffffff;padding:24px 28px;border-radius:18px 18px 0 0;">
            <div style="font-size:13px;letter-spacing:0.12em;text-transform:uppercase;color:#d6c4a1;">Zbrojnice</div>
            <h1 style="margin:10px 0 0;font-size:28px;line-height:1.2;">{{encodedTitle}}</h1>
        </div>
        <div style="background:#ffffff;padding:28px;border:1px solid #e5dfd2;border-top:none;border-radius:0 0 18px 18px;">
            <p style="margin:0 0 18px;font-size:15px;line-height:1.7;">{{encodedMessage}}</p>
            <div style="display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px;margin:0 0 24px;">
                <div style="border:1px solid #e5dfd2;background:#fcfbf8;padding:14px 16px;border-radius:12px;">
                    <div style="font-size:11px;letter-spacing:0.08em;text-transform:uppercase;color:#8a6f39;margin-bottom:6px;">Objednávka</div>
                    <strong style="font-size:18px;">{{orderNumber}}</strong>
                </div>
                <div style="border:1px solid #e5dfd2;background:#fcfbf8;padding:14px 16px;border-radius:12px;">
                    <div style="font-size:11px;letter-spacing:0.08em;text-transform:uppercase;color:#8a6f39;margin-bottom:6px;">Vytvořeno</div>
                    <strong style="font-size:18px;">{{createdAt}}</strong>
                </div>
                <div style="border:1px solid #e5dfd2;background:#fcfbf8;padding:14px 16px;border-radius:12px;">
                    <div style="font-size:11px;letter-spacing:0.08em;text-transform:uppercase;color:#8a6f39;margin-bottom:6px;">Celkem</div>
                    <strong style="font-size:18px;">{{totalPrice}}</strong>
                </div>
            </div>
            {{invoiceBlock}}
            <table style="width:100%;border-collapse:collapse;margin-bottom:24px;">
                <thead>
                    <tr>
                        <th style="background:#182230;color:#ffffff;padding:12px;text-align:left;font-size:12px;text-transform:uppercase;letter-spacing:0.06em;">Položka</th>
                        <th style="background:#182230;color:#ffffff;padding:12px;text-align:right;font-size:12px;text-transform:uppercase;letter-spacing:0.06em;">Množství</th>
                        <th style="background:#182230;color:#ffffff;padding:12px;text-align:right;font-size:12px;text-transform:uppercase;letter-spacing:0.06em;">Cena</th>
                    </tr>
                </thead>
                <tbody>
                    {{rows}}
                </tbody>
            </table>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;">
                <div style="border:1px solid #e5dfd2;background:#fcfbf8;padding:16px;border-radius:12px;">
                    <div style="font-size:11px;letter-spacing:0.08em;text-transform:uppercase;color:#8a6f39;margin-bottom:8px;">Dodací údaje</div>
                    <div>{{shippingName}}</div>
                    <div>{{shippingStreet}}</div>
                    <div>{{shippingCity}}</div>
                </div>
                <div style="border:1px solid #e5dfd2;background:#fcfbf8;padding:16px;border-radius:12px;">
                    <div style="font-size:11px;letter-spacing:0.08em;text-transform:uppercase;color:#8a6f39;margin-bottom:8px;">Fakturační údaje</div>
                    <div>{{billingName}}</div>
                    <div>{{billingStreet}}</div>
                    <div>{{billingCity}}</div>
                </div>
            </div>
            <p style="margin:24px 0 0;font-size:13px;line-height:1.6;color:#6b7280;">Tento e-mail byl odeslán automaticky z aplikace Zbrojnice.</p>
        </div>
    </div>
</body>
</html>
""";
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
            throw new InvalidOperationException("Před přidáním zbraní do košíku nahrajte občanský průkaz nebo nákupní povolení.");
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
        order.CustomerNote = checkoutDetails.CustomerNote?.Trim() ?? string.Empty;

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
