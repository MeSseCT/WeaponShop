using Microsoft.EntityFrameworkCore;
using WeaponShop.Application.Interfaces;
using WeaponShop.Application.Services;
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
            .Include(item => item.Images)
            .AsNoTracking()
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AccessoryNavigationItem>> GetAvailableNavigationItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Accessories
            .AsNoTracking()
            .Where(item => item.IsAvailable)
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Name)
            .Select(item => new AccessoryNavigationItem
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.Category
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Accessory?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Accessories
            .Include(item => item.Images)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<List<Accessory>> GetByIdsForUpdateAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        return await _context.Accessories
            .Include(item => item.Images)
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
        var existing = await _context.Accessories
            .Include(item => item.Images)
            .SingleAsync(item => item.Id == accessory.Id, cancellationToken);

        existing.Name = accessory.Name;
        existing.Category = accessory.Category;
        existing.Description = accessory.Description;
        existing.Price = accessory.Price;
        existing.StockQuantity = accessory.StockQuantity;
        existing.IsAvailable = accessory.IsAvailable;
        existing.Manufacturer = accessory.Manufacturer;
        existing.ImageFileName = accessory.ImageFileName;

        var incomingImages = accessory.Images
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
                existing.Images.Add(new AccessoryImage
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
        var existing = await _context.Accessories
            .Include(item => item.Images)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (existing is null)
        {
            return;
        }

        _context.Accessories.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
