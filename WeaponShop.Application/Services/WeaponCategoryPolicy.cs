using WeaponShop.Domain.Identity;

namespace WeaponShop.Application.Services;

public static class WeaponCategoryPolicy
{
    public const string CategoryB = "B";
    public const string CategoryC = "C";
    public const string CategoryCI = "CI";
    public const string CategoryD = "D";

    public static readonly IReadOnlyList<string> SellableCategories = [CategoryB, CategoryC, CategoryCI, CategoryD];

    public static string NormalizeCategoryCode(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return string.Empty;
        }

        return category
            .Trim()
            .ToUpperInvariant()
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }

    public static string ToDisplayCode(string? category)
    {
        return NormalizeCategoryCode(category) switch
        {
            "AI" => "A-I",
            CategoryCI => "C-I",
            var normalized when string.IsNullOrWhiteSpace(normalized) => "?",
            var normalized => normalized
        };
    }

    public static bool IsSellableCategory(string? category)
    {
        return NormalizeCategoryCode(category) is CategoryB or CategoryC or CategoryCI or CategoryD;
    }

    public static bool RequiresManualApproval(string? category)
    {
        return NormalizeCategoryCode(category) switch
        {
            CategoryD => false,
            CategoryB or CategoryC or CategoryCI => true,
            _ => true
        };
    }

    public static bool CanBrowseRestrictedCatalog(ApplicationUser? user, bool isStaff)
    {
        return isStaff || HasRequiredAge(user);
    }

    public static WeaponCategoryAccessResult EvaluateAccess(ApplicationUser? user, string? category, bool isStaff)
    {
        var normalizedCategory = NormalizeCategoryCode(category);
        if (!IsSellableCategory(normalizedCategory))
        {
            return Denied(
                GetAccessLabel(normalizedCategory),
                "Kategorie A a A-I jsou zakázané a aplikace je neprodává.");
        }

        if (isStaff)
        {
            return Allowed(GetAccessLabel(normalizedCategory));
        }

        if (user is null)
        {
            return Denied(
                GetAccessLabel(normalizedCategory),
                "Pro práci s kontrolovaným sortimentem se nejprve přihlaste.");
        }

        if (!HasRequiredAge(user))
        {
            return Denied(
                GetAccessLabel(normalizedCategory),
                $"Pro kategorii {ToDisplayCode(normalizedCategory)} musíte být starší 18 let.");
        }

        return normalizedCategory switch
        {
            CategoryD => Allowed(GetAccessLabel(normalizedCategory)),
            CategoryCI when !HasIdentityCard(user) => Denied(GetAccessLabel(normalizedCategory), "Pro kategorii C-I nahrajte doklad totožnosti."),
            CategoryCI when !user.IdCardIssuedInCzechRepublic => Denied(GetAccessLabel(normalizedCategory), "Pro kategorii C-I je vyžadován občanský průkaz vydaný v České republice."),
            CategoryCI => Allowed(GetAccessLabel(normalizedCategory)),
            CategoryC when !HasIdentityCard(user) => Denied(GetAccessLabel(normalizedCategory), "Pro kategorii C nahrajte doklad totožnosti."),
            CategoryC when !user.FirearmsLicenseRecorded => Denied(GetAccessLabel(normalizedCategory), "Pro kategorii C musí být u profilu evidováno oprávnění v systému."),
            CategoryC => Allowed(GetAccessLabel(normalizedCategory)),
            CategoryB when !HasIdentityCard(user) => Denied(GetAccessLabel(normalizedCategory), "Pro kategorii B nahrajte doklad totožnosti."),
            CategoryB when !HasPurchasePermit(user) => Denied(GetAccessLabel(normalizedCategory), "Pro kategorii B nahrajte potvrzení nebo nákupní povolení."),
            CategoryB => Allowed(GetAccessLabel(normalizedCategory)),
            _ => Denied(GetAccessLabel(normalizedCategory), "Tuto kategorii nelze v aplikaci zpracovat.")
        };
    }

    public static string GetAccessLabel(string? category)
    {
        return NormalizeCategoryCode(category) switch
        {
            CategoryB => "Kategorie B: 18+, doklad totožnosti a potvrzení / nákupní povolení",
            CategoryC => "Kategorie C: 18+, doklad totožnosti a oprávnění evidované v systému",
            CategoryCI => "Kategorie C-I: 18+ a občanský průkaz vydaný v České republice",
            CategoryD => "Kategorie D: pouze věk 18+",
            "AI" or "A" => "Kategorie A a A-I: zakázané kategorie",
            _ => "Kontrolovaný sortiment podle právní kategorie"
        };
    }

    public static bool HasRequiredAge(ApplicationUser? user)
    {
        if (user?.DateOfBirth is not { } dateOfBirth)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (today < dateOfBirth.AddYears(age))
        {
            age--;
        }

        return age >= 18;
    }

    public static bool HasIdentityCard(ApplicationUser? user)
    {
        return user is not null && !string.IsNullOrWhiteSpace(user.IdCardFileName);
    }

    public static bool HasPurchasePermit(ApplicationUser? user)
    {
        return user is not null && !string.IsNullOrWhiteSpace(user.PurchasePermitFileName);
    }

    private static WeaponCategoryAccessResult Allowed(string accessLabel)
    {
        return new WeaponCategoryAccessResult
        {
            CanViewDetails = true,
            CanAddToCart = true,
            AccessLabel = accessLabel
        };
    }

    private static WeaponCategoryAccessResult Denied(string accessLabel, string message)
    {
        return new WeaponCategoryAccessResult
        {
            AccessLabel = accessLabel,
            RestrictionMessage = message
        };
    }
}

public sealed class WeaponCategoryAccessResult
{
    public bool CanViewDetails { get; init; }
    public bool CanAddToCart { get; init; }
    public string AccessLabel { get; init; } = string.Empty;
    public string RestrictionMessage { get; init; } = string.Empty;
}
