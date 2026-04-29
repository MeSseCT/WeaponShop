using WeaponShop.Application.Services;
using WeaponShop.Domain.Identity;

namespace WeaponShop.Tests.Services;

public class WeaponCategoryPolicyTests
{
    [Fact]
    public void RequiresManualApproval_CategoryD_ReturnsFalse()
    {
        Assert.False(WeaponCategoryPolicy.RequiresManualApproval("D"));
    }

    [Fact]
    public void RequiresManualApproval_CategoryB_ReturnsTrue()
    {
        Assert.True(WeaponCategoryPolicy.RequiresManualApproval("B"));
    }

    [Fact]
    public void EvaluateAccess_CategoryD_WithAdultUser_AllowsPurchaseWithoutDocuments()
    {
        var user = new ApplicationUser
        {
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-25)
        };

        var access = WeaponCategoryPolicy.EvaluateAccess(user, "D", isStaff: false);

        Assert.True(access.CanViewDetails);
        Assert.True(access.CanAddToCart);
    }
}
