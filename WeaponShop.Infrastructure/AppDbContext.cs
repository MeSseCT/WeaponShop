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
            builder.Property(u => u.DriverLicenseFileName).HasColumnName("driver_license_file_name");
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
            builder.Property(w => w.Category).HasColumnName("legal_category");
            builder.Property(w => w.Description).HasColumnName("weapon_description");
            builder.Property(w => w.Price).HasColumnName("price_amount");
            builder.Property(w => w.Manufacturer).HasColumnName("manufacturer_name");
            builder.Property(w => w.StockQuantity).HasColumnName("stock_quantity");
            builder.Property(w => w.IsAvailable).HasColumnName("is_available");
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("purchase_requests");
            builder.Property(o => o.Id).HasColumnName("request_id");
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
            builder.Property(oi => oi.Quantity).HasColumnName("quantity");
            builder.Property(oi => oi.UnitPrice).HasColumnName("unit_price");

            builder.HasOne(oi => oi.Weapon)
                .WithMany()
                .HasForeignKey(oi => oi.WeaponId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(oi => new { oi.OrderId, oi.WeaponId })
                .IsUnique();

            builder.Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString =
            Environment.GetEnvironmentVariable("WEAPONSHOP_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=weaponshop;Username=weaponshop;Password=weaponshop_dev_password;Include Error Detail=true";

        optionsBuilder.UseNpgsql(connectionString);
    }
}
