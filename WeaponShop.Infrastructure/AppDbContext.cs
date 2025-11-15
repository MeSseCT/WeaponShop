using Microsoft.EntityFrameworkCore;
using WeaponShop.Domain;

namespace WeaponShop.Infrastructure;

//
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Weapon> Weapons => Set<Weapon>();
}
