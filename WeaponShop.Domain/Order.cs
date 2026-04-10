using WeaponShop.Domain.Identity;

namespace WeaponShop.Domain;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public DateTime? WarehouseCheckedAtUtc { get; set; }
    public DateTime? GunsmithCheckedAtUtc { get; set; }
    public DateTime? WarehousePreparedAtUtc { get; set; }
    public DateTime? ShippedAtUtc { get; set; }
    public DateTime? ReadyForPickupAtUtc { get; set; }
    public DateTime? PickupHandedOverAtUtc { get; set; }
    public DateTime? StockReservedAtUtc { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingStreet { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string BillingName { get; set; } = string.Empty;
    public string BillingStreet { get; set; } = string.Empty;
    public string BillingCity { get; set; } = string.Empty;
    public string BillingPostalCode { get; set; } = string.Empty;
    public string CustomerNote { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderAudit> Audits { get; set; } = new List<OrderAudit>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
