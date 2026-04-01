using Microsoft.EntityFrameworkCore;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain.Identity;

namespace WeaponShop.Infrastructure.Repositories;

public class ApplicationUserRepository : IApplicationUserRepository
{
    private readonly AppDbContext _context;

    public ApplicationUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }
}
