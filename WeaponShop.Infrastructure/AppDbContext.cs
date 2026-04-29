using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WeaponShop.Domain;
using WeaponShop.Domain.Identity;

namespace WeaponShop.Infrastructure;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Weapon> Weapons => Set<Weapon>();
    public DbSet<WeaponImage> WeaponImages => Set<WeaponImage>();
    public DbSet<WeaponUnit> WeaponUnits => Set<WeaponUnit>();
    public DbSet<WeaponUnitPart> WeaponUnitParts => Set<WeaponUnitPart>();
    public DbSet<Accessory> Accessories => Set<Accessory>();
    public DbSet<AccessoryImage> AccessoryImages => Set<AccessoryImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderAudit> OrderAudits => Set<OrderAudit>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.ToTable("users");
            builder.Property(u => u.FirstName).HasColumnName("first_name");
            builder.Property(u => u.LastName).HasColumnName("last_name");
            builder.Property(u => u.DateOfBirth).HasColumnName("date_of_birth");
            builder.Property(u => u.IdCardFileName).HasColumnName("id_card_file_name");
            builder.Property(u => u.IdCardIssuedInCzechRepublic).HasColumnName("id_card_issued_in_czech_republic");
            builder.Property(u => u.FirearmsLicenseRecorded).HasColumnName("firearms_license_recorded");
            builder.Property(u => u.PurchasePermitFileName).HasColumnName("purchase_permit_file_name");
            builder.Property(u => u.DocumentsUpdatedAt).HasColumnName("documents_updated_at_utc");
            builder.Property(u => u.DocumentsUploadWindowStartedAtUtc).HasColumnName("documents_upload_window_start_utc");
            builder.Property(u => u.DocumentsUploadCount).HasColumnName("documents_upload_count");
        });

        modelBuilder.Entity<IdentityRole>(builder => builder.ToTable("roles"));
        modelBuilder.Entity<IdentityRoleClaim<string>>(builder => builder.ToTable("role_claims"));
        modelBuilder.Entity<IdentityUserClaim<string>>(builder => builder.ToTable("user_claims"));
        modelBuilder.Entity<IdentityUserLogin<string>>(builder => builder.ToTable("user_logins"));
        modelBuilder.Entity<IdentityUserRole<string>>(builder => builder.ToTable("user_roles"));
        modelBuilder.Entity<IdentityUserToken<string>>(builder => builder.ToTable("user_tokens"));

        modelBuilder.Entity<Weapon>(builder =>
        {
            builder.ToTable("weapons");
            builder.Property(w => w.Id).HasColumnName("weapon_id");
            builder.Property(w => w.Name).HasColumnName("weapon_name");
            builder.Property(w => w.TypeDesignation).HasColumnName("type_designation");
            builder.Property(w => w.Category).HasColumnName("legal_category");
            builder.Property(w => w.Description).HasColumnName("weapon_description");
            builder.Property(w => w.Price).HasColumnName("price_amount");
            builder.Property(w => w.Manufacturer).HasColumnName("manufacturer_name");
            builder.Property(w => w.Caliber).HasColumnName("caliber_value");
            builder.Property(w => w.PrimarySerialNumber).HasColumnName("primary_serial_number");
            builder.Property(w => w.StockQuantity).HasColumnName("stock_quantity");
            builder.Property(w => w.IsAvailable).HasColumnName("is_available");
            builder.Property(w => w.ImageFileName).HasColumnName("image_file_name");

            builder.HasMany(w => w.Images)
                .WithOne(image => image.Weapon)
                .HasForeignKey(image => image.WeaponId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(w => w.Units)
                .WithOne(unit => unit.Weapon)
                .HasForeignKey(unit => unit.WeaponId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeaponImage>(builder =>
        {
            builder.ToTable("weapon_images");
            builder.Property(image => image.Id).HasColumnName("weapon_image_id");
            builder.Property(image => image.WeaponId).HasColumnName("weapon_id");
            builder.Property(image => image.FileName).HasColumnName("image_file_name").HasMaxLength(260);
            builder.Property(image => image.SortOrder).HasColumnName("sort_order");

            builder.HasIndex(image => new { image.WeaponId, image.SortOrder });
        });

        modelBuilder.Entity<WeaponUnit>(builder =>
        {
            builder.ToTable("weapon_units");
            builder.Property(unit => unit.Id).HasColumnName("weapon_unit_id");
            builder.Property(unit => unit.WeaponId).HasColumnName("weapon_id");
            builder.Property(unit => unit.PrimarySerialNumber).HasColumnName("primary_serial_number").HasMaxLength(120);
            builder.Property(unit => unit.Status).HasColumnName("unit_status");
            builder.Property(unit => unit.ReservedOrderId).HasColumnName("reserved_order_id");
            builder.Property(unit => unit.SoldOrderId).HasColumnName("sold_order_id");
            builder.Property(unit => unit.Notes).HasColumnName("notes").HasMaxLength(300);

            builder.HasIndex(unit => unit.PrimarySerialNumber)
                .IsUnique();

            builder.HasIndex(unit => new { unit.WeaponId, unit.Status });

            builder.HasOne(unit => unit.ReservedOrder)
                .WithMany()
                .HasForeignKey(unit => unit.ReservedOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(unit => unit.SoldOrder)
                .WithMany()
                .HasForeignKey(unit => unit.SoldOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WeaponUnitPart>(builder =>
        {
            builder.ToTable("weapon_unit_parts");
            builder.Property(part => part.Id).HasColumnName("weapon_unit_part_id");
            builder.Property(part => part.WeaponUnitId).HasColumnName("weapon_unit_id");
            builder.Property(part => part.SlotNumber).HasColumnName("slot_number");
            builder.Property(part => part.PartName).HasColumnName("part_name").HasMaxLength(120);
            builder.Property(part => part.SerialNumber).HasColumnName("serial_number").HasMaxLength(120);
            builder.Property(part => part.Notes).HasColumnName("notes").HasMaxLength(300);

            builder.HasIndex(part => part.SerialNumber)
                .IsUnique();

            builder.HasIndex(part => new { part.WeaponUnitId, part.SlotNumber })
                .IsUnique();

            builder.HasOne(part => part.WeaponUnit)
                .WithMany(unit => unit.Parts)
                .HasForeignKey(part => part.WeaponUnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Accessory>(builder =>
        {
            builder.ToTable("catalog_accessories");
            builder.Property(a => a.Id).HasColumnName("accessory_id");
            builder.Property(a => a.Name).HasColumnName("accessory_name");
            builder.Property(a => a.Category).HasColumnName("catalog_category");
            builder.Property(a => a.Description).HasColumnName("accessory_description");
            builder.Property(a => a.Price).HasColumnName("price_amount");
            builder.Property(a => a.Manufacturer).HasColumnName("manufacturer_name");
            builder.Property(a => a.StockQuantity).HasColumnName("stock_quantity");
            builder.Property(a => a.IsAvailable).HasColumnName("is_available");
            builder.Property(a => a.ImageFileName).HasColumnName("image_file_name");

            builder.HasMany(a => a.Images)
                .WithOne(image => image.Accessory)
                .HasForeignKey(image => image.AccessoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccessoryImage>(builder =>
        {
            builder.ToTable("accessory_images");
            builder.Property(image => image.Id).HasColumnName("accessory_image_id");
            builder.Property(image => image.AccessoryId).HasColumnName("accessory_id");
            builder.Property(image => image.FileName).HasColumnName("image_file_name").HasMaxLength(260);
            builder.Property(image => image.SortOrder).HasColumnName("sort_order");

            builder.HasIndex(image => new { image.AccessoryId, image.SortOrder });
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("purchase_requests");
            builder.Property(o => o.Id).HasColumnName("request_id");
            builder.Property(o => o.OrderNumber).HasColumnName("order_number").HasMaxLength(32);
            builder.Property(o => o.UserId).HasColumnName("customer_user_id");
            builder.Property(o => o.CreatedAt).HasColumnName("created_at_utc");
            builder.Property(o => o.Status).HasColumnName("request_status");
            builder.Property(o => o.ApprovedAtUtc).HasColumnName("approved_at_utc");
            builder.Property(o => o.RejectedAtUtc).HasColumnName("rejected_at_utc");
            builder.Property(o => o.WarehouseCheckedAtUtc).HasColumnName("warehouse_checked_at_utc");
            builder.Property(o => o.GunsmithCheckedAtUtc).HasColumnName("gunsmith_checked_at_utc");
            builder.Property(o => o.WarehousePreparedAtUtc).HasColumnName("warehouse_prepared_at_utc");
            builder.Property(o => o.ShippedAtUtc).HasColumnName("shipped_at_utc");
            builder.Property(o => o.ReadyForPickupAtUtc).HasColumnName("ready_for_pickup_at_utc");
            builder.Property(o => o.PickupHandedOverAtUtc).HasColumnName("pickup_handed_over_at_utc");
            builder.Property(o => o.StockReservedAtUtc).HasColumnName("stock_reserved_at_utc");
            builder.Property(o => o.ContactEmail).HasColumnName("contact_email").HasMaxLength(200);
            builder.Property(o => o.ContactPhone).HasColumnName("contact_phone").HasMaxLength(50);
            builder.Property(o => o.DeliveryMethod).HasColumnName("delivery_method").HasMaxLength(50);
            builder.Property(o => o.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50);
            builder.Property(o => o.ShippingName).HasColumnName("shipping_name").HasMaxLength(200);
            builder.Property(o => o.ShippingStreet).HasColumnName("shipping_street").HasMaxLength(200);
            builder.Property(o => o.ShippingCity).HasColumnName("shipping_city").HasMaxLength(100);
            builder.Property(o => o.ShippingPostalCode).HasColumnName("shipping_postal_code").HasMaxLength(20);
            builder.Property(o => o.BillingName).HasColumnName("billing_name").HasMaxLength(200);
            builder.Property(o => o.BillingStreet).HasColumnName("billing_street").HasMaxLength(200);
            builder.Property(o => o.BillingCity).HasColumnName("billing_city").HasMaxLength(100);
            builder.Property(o => o.BillingPostalCode).HasColumnName("billing_postal_code").HasMaxLength(20);
            builder.Property(o => o.CustomerNote).HasColumnName("customer_note").HasMaxLength(1000);
            builder.Property(o => o.TotalPrice).HasColumnName("total_amount");

            builder.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

            builder.HasIndex(o => o.OrderNumber)
                .IsUnique();
        });

        modelBuilder.Entity<OrderAudit>(builder =>
        {
            builder.ToTable("purchase_request_audits");
            builder.Property(a => a.Id).HasColumnName("audit_id");
            builder.Property(a => a.OrderId).HasColumnName("request_id");
            builder.Property(a => a.FromStatus).HasColumnName("from_status");
            builder.Property(a => a.ToStatus).HasColumnName("to_status");
            builder.Property(a => a.Action).HasColumnName("action_name").HasMaxLength(200);
            builder.Property(a => a.ActorUserId).HasColumnName("actor_user_id");
            builder.Property(a => a.ActorName).HasColumnName("actor_name").HasMaxLength(200);
            builder.Property(a => a.ActorRole).HasColumnName("actor_role").HasMaxLength(50);
            builder.Property(a => a.OccurredAtUtc).HasColumnName("occurred_at_utc");
            builder.Property(a => a.Notes).HasColumnName("notes");

            builder.HasOne(a => a.Order)
                .WithMany(o => o.Audits)
                .HasForeignKey(a => a.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(a => new { a.OrderId, a.OccurredAtUtc });
        });

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("user_notifications");
            builder.Property(n => n.Id).HasColumnName("notification_id");
            builder.Property(n => n.UserId).HasColumnName("user_id");
            builder.Property(n => n.OrderId).HasColumnName("request_id");
            builder.Property(n => n.Title).HasColumnName("title").HasMaxLength(200);
            builder.Property(n => n.Message).HasColumnName("message").HasMaxLength(1000);
            builder.Property(n => n.CreatedAtUtc).HasColumnName("created_at_utc");
            builder.Property(n => n.IsRead).HasColumnName("is_read");

            builder.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.Order)
                .WithMany(o => o.Notifications)
                .HasForeignKey(n => n.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(n => new { n.UserId, n.CreatedAtUtc });
            builder.HasIndex(n => new { n.OrderId, n.CreatedAtUtc });
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.ToTable("purchase_request_items");
            builder.Property(oi => oi.Id).HasColumnName("request_item_id");
            builder.Property(oi => oi.OrderId).HasColumnName("request_id");
            builder.Property(oi => oi.WeaponId).HasColumnName("weapon_id");
            builder.Property(oi => oi.AccessoryId).HasColumnName("accessory_id");
            builder.Property(oi => oi.Quantity).HasColumnName("quantity");
            builder.Property(oi => oi.UnitPrice).HasColumnName("unit_price");

            builder.HasOne(oi => oi.Weapon)
                .WithMany()
                .HasForeignKey(oi => oi.WeaponId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(oi => oi.Accessory)
                .WithMany()
                .HasForeignKey(oi => oi.AccessoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(oi => new { oi.OrderId, oi.WeaponId })
                .IsUnique();
            builder.HasIndex(oi => new { oi.OrderId, oi.AccessoryId })
                .IsUnique();

            builder.Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_purchase_request_items_exactly_one_catalog_item",
                    """
                    (
                        (CASE WHEN weapon_id IS NULL THEN 0 ELSE 1 END) +
                        (CASE WHEN accessory_id IS NULL THEN 0 ELSE 1 END)
                    ) = 1
                    """);
                tableBuilder.HasCheckConstraint(
                    "CK_purchase_request_items_quantity_positive",
                    "quantity > 0");
            });
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            throw new InvalidOperationException(
                "AppDbContext nebyl nakonfigurován. Použijte AddDbContext() nebo design-time factory s nastaveným connection stringem.");
        }
    }
}
