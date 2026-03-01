using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(builder =>
        {
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

        modelBuilder.Entity<OrderItem>(builder =>
        {
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
