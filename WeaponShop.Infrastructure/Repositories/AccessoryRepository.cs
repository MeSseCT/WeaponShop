using Microsoft.EntityFrameworkCore;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;

namespace WeaponShop.Infrastructure.Repositories;

public class AccessoryRepository : IAccessoryRepository
{
    private readonly AppDbContext _context;

    public AccessoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Accessory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Accessories
            .AsNoTracking()
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Accessory?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Accessories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<List<Accessory>> GetByIdsForUpdateAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        return await _context.Accessories
            .Where(item => ids.Contains(item.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Accessory accessory, CancellationToken cancellationToken = default)
    {
        await _context.Accessories.AddAsync(accessory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Accessory accessory, CancellationToken cancellationToken = default)
    {
        _context.Accessories.Update(accessory);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Accessories.FindAsync(new object[] { id }, cancellationToken);
        if (existing is null)
        {
            return;
        }

        _context.Accessories.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
