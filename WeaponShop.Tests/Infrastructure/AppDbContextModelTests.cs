using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WeaponShop.Domain;
using WeaponShop.Infrastructure;

namespace WeaponShop.Tests.Infrastructure;

public class AppDbContextModelTests
{
    [Fact]
    public void OrderItem_HasIntegrityCheckConstraints()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=weaponshop_test;Username=test;Password=test")
            .Options;

        using var context = new AppDbContext(options);
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var entityType = designTimeModel.FindEntityType(typeof(OrderItem));

        Assert.NotNull(entityType);

        var constraintNames = entityType!
            .GetCheckConstraints()
            .Select(constraint => constraint.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("CK_purchase_request_items_exactly_one_catalog_item", constraintNames);
        Assert.Contains("CK_purchase_request_items_quantity_positive", constraintNames);
    }
}
