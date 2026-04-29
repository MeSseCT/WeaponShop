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
            .Include(w => w.Units)
            .ThenInclude(unit => unit.Parts)
            .AsNoTracking()
            .AsSplitQuery()
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WeaponNavigationItem>> GetAvailableNavigationItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .AsNoTracking()
            .Where(item => item.IsAvailable && item.StockQuantity > 0)
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
            .Include(w => w.Units)
            .ThenInclude(unit => unit.Parts)
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<WeaponUnit?> GetUnitByIdAsync(int weaponId, int unitId, CancellationToken cancellationToken = default)
    {
        return await _context.WeaponUnits
            .Include(unit => unit.Parts)
            .AsNoTracking()
            .SingleOrDefaultAsync(unit => unit.WeaponId == weaponId && unit.Id == unitId, cancellationToken);
    }

    public async Task<List<Weapon>> GetByIdsForUpdateAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .Include(w => w.Images)
            .Include(w => w.Units)
            .ThenInclude(unit => unit.Parts)
            .AsSplitQuery()
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
            .Include(w => w.Units)
            .ThenInclude(unit => unit.Parts)
            .AsSplitQuery()
            .SingleAsync(w => w.Id == weapon.Id, cancellationToken);

        existing.Name = weapon.Name;
        existing.TypeDesignation = weapon.TypeDesignation;
        existing.Category = weapon.Category;
        existing.Description = weapon.Description;
        existing.Price = weapon.Price;
        existing.IsAvailable = weapon.IsAvailable;
        existing.Manufacturer = weapon.Manufacturer;
        existing.Caliber = weapon.Caliber;
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

    public async Task AddUnitAsync(int weaponId, WeaponUnit unit, CancellationToken cancellationToken = default)
    {
        var weapon = await _context.Weapons
            .Include(item => item.Units)
            .SingleAsync(item => item.Id == weaponId, cancellationToken);

        unit.WeaponId = weaponId;
        unit.Status = WeaponUnitStatus.InStock;
        weapon.Units.Add(unit);
        weapon.StockQuantity += 1;
        weapon.IsAvailable = weapon.StockQuantity > 0;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUnitAsync(int weaponId, WeaponUnit unit, CancellationToken cancellationToken = default)
    {
        var weapon = await _context.Weapons
            .Include(item => item.Units)
            .ThenInclude(existingUnit => existingUnit.Parts)
            .AsSplitQuery()
            .SingleAsync(item => item.Id == weaponId, cancellationToken);

        var existingUnit = weapon.Units.SingleOrDefault(item => item.Id == unit.Id);
        if (existingUnit is null)
        {
            throw new KeyNotFoundException("Evidovaný kus zbraně nebyl nalezen.");
        }

        existingUnit.PrimarySerialNumber = unit.PrimarySerialNumber;
        existingUnit.Notes = unit.Notes;

        var removedParts = existingUnit.Parts
            .Where(part => unit.Parts.All(incoming => incoming.SlotNumber != part.SlotNumber))
            .ToList();

        foreach (var removedPart in removedParts)
        {
            existingUnit.Parts.Remove(removedPart);
        }

        foreach (var incomingPart in unit.Parts.OrderBy(part => part.SlotNumber))
        {
            var trackedPart = existingUnit.Parts.SingleOrDefault(part => part.SlotNumber == incomingPart.SlotNumber);
            if (trackedPart is null)
            {
                existingUnit.Parts.Add(new WeaponUnitPart
                {
                    SlotNumber = incomingPart.SlotNumber,
                    PartName = incomingPart.PartName,
                    SerialNumber = incomingPart.SerialNumber,
                    Notes = incomingPart.Notes
                });
                continue;
            }

            trackedPart.PartName = incomingPart.PartName;
            trackedPart.SerialNumber = incomingPart.SerialNumber;
            trackedPart.Notes = incomingPart.Notes;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUnitAsync(int weaponId, int unitId, CancellationToken cancellationToken = default)
    {
        var weapon = await _context.Weapons
            .Include(item => item.Units)
            .ThenInclude(unit => unit.Parts)
            .AsSplitQuery()
            .SingleAsync(item => item.Id == weaponId, cancellationToken);

        var unit = weapon.Units.SingleOrDefault(item => item.Id == unitId);
        if (unit is null)
        {
            return;
        }

        if (unit.Status is WeaponUnitStatus.Reserved or WeaponUnitStatus.Sold)
        {
            throw new InvalidOperationException("Rezervovaný nebo prodaný kus nelze smazat.");
        }

        weapon.Units.Remove(unit);
        if (weapon.StockQuantity > 0)
        {
            weapon.StockQuantity -= 1;
        }

        weapon.IsAvailable = weapon.StockQuantity > 0;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Weapons
            .Include(w => w.Images)
            .Include(w => w.Units)
            .ThenInclude(unit => unit.Parts)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (existing is null)
        {
            return;
        }

        _context.Weapons.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
