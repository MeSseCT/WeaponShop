using Microsoft.EntityFrameworkCore;
using WeaponShop.Application.Interfaces;
using WeaponShop.Application.Services;
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
            .Include(w => w.Images)
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WeaponNavigationItem>> GetAvailableNavigationItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .AsNoTracking()
            .Where(item => item.IsAvailable)
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Manufacturer)
            .Select(item => new WeaponNavigationItem
            {
                Category = item.Category,
                Manufacturer = item.Manufacturer
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Weapon?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .Include(w => w.Images)
            .AsNoTracking()
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<List<Weapon>> GetByIdsForUpdateAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .Include(w => w.Images)
            .Where(w => ids.Contains(w.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Weapon weapon, CancellationToken cancellationToken = default)
    {
        await _context.Weapons.AddAsync(weapon, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Weapon weapon, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Weapons
            .Include(w => w.Images)
            .SingleAsync(w => w.Id == weapon.Id, cancellationToken);

        existing.Name = weapon.Name;
        existing.Category = weapon.Category;
        existing.Description = weapon.Description;
        existing.Price = weapon.Price;
        existing.StockQuantity = weapon.StockQuantity;
        existing.IsAvailable = weapon.IsAvailable;
        existing.Manufacturer = weapon.Manufacturer;
        existing.ImageFileName = weapon.ImageFileName;

        var incomingImages = weapon.Images
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.Id)
            .ToList();

        var removedImages = existing.Images
            .Where(existingImage => incomingImages.All(image => image.Id != existingImage.Id))
            .ToList();

        foreach (var removedImage in removedImages)
        {
            existing.Images.Remove(removedImage);
        }

        foreach (var incomingImage in incomingImages)
        {
            var trackedImage = existing.Images.SingleOrDefault(image => image.Id == incomingImage.Id);
            if (trackedImage is null)
            {
                existing.Images.Add(new WeaponImage
                {
                    FileName = incomingImage.FileName,
                    SortOrder = incomingImage.SortOrder
                });
                continue;
            }

            trackedImage.FileName = incomingImage.FileName;
            trackedImage.SortOrder = incomingImage.SortOrder;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Weapons
            .Include(w => w.Images)
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (existing is null)
        {
            return;
        }

        _context.Weapons.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
