using Microsoft.EntityFrameworkCore;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;

namespace WeaponShop.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the weapon repository.
/// Handles all database operations for weapons.
/// </summary>
public class WeaponRepository : IWeaponRepository
{
    private readonly AppDbContext _context;

    public WeaponRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Weapon>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Weapon?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .AsNoTracking()
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task AddAsync(Weapon weapon, CancellationToken cancellationToken = default)
    {
        await _context.Weapons.AddAsync(weapon, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Weapon weapon, CancellationToken cancellationToken = default)
    {
        _context.Weapons.Update(weapon);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Weapons.FindAsync(new object[] { id }, cancellationToken);
        if (existing is null)
        {
            return;
        }

        _context.Weapons.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

