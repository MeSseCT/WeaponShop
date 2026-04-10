using WeaponShop.Domain;

namespace WeaponShop.Web.Helpers;

public static class CatalogPresentation
{
    public static string GetAccessoryCategoryLabel(string? category)
    {
        return Normalize(category) switch
        {
            "optics" => "Optika",
            "storage" => "Pouzdra a uložení",
            "protection" => "Ochranné potřeby",
            "selfdefense" => "Sebeobrana",
            "airguns" => "Vzduchovky",
            _ => string.IsNullOrWhiteSpace(category) ? "Doplnky" : category.Trim()
        };
    }

    public static int GetAccessoryCategorySortKey(string? category)
    {
        return Normalize(category) switch
        {
            "optics" => 10,
            "storage" => 20,
            "protection" => 30,
            "selfdefense" => 40,
            "airguns" => 50,
            _ => 100
        };
    }

    public static string GetWeaponCategoryLabel(string? category)
    {
        var code = string.IsNullOrWhiteSpace(category)
            ? "?"
            : category.Trim().ToUpperInvariant();

        return $"Kategorie {code}";
    }

    public static int GetWeaponCategorySortKey(string? category)
    {
        var code = string.IsNullOrWhiteSpace(category)
            ? 'Z'
            : char.ToUpperInvariant(category.Trim()[0]);

        return code switch
        {
            'A' => 10,
            'B' => 20,
            'C' => 30,
            'D' => 40,
            'E' => 50,
            _ => 100
        };
    }

    public static string GetWeaponAccessLabel()
    {
        return "Pouze po prihlaseni, overeni veku 18+ a nahrani dokladu";
    }

    public static string GetAccessoryAccessLabel()
    {
        return "Volne dostupne zbozi bez vekove kontroly";
    }

    public static string GetWeaponFallbackDescription()
    {
        return "Zbran s kontrolovanym prodejem a navaznym schvalovacim workflow.";
    }

    public static string GetAccessoryFallbackDescription()
    {
        return "Verejne dostupna polozka e-shopu bez nutnosti vekoveho overeni.";
    }

    public static string GetStatusLabel(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Created => "V kosiku",
            OrderStatus.AwaitingApproval => "Ceka na schvaleni",
            OrderStatus.Approved => "Schvaleno",
            OrderStatus.Rejected => "Zamitnuto",
            OrderStatus.Completed => "Dokonceno",
            OrderStatus.AwaitingGunsmith => "Ceka na zbrojire",
            OrderStatus.AwaitingDispatch => "Pripraveno k expedici",
            OrderStatus.Shipped => "Odeslano",
            OrderStatus.ReadyForPickup => "Pripraveno k vyzvednuti",
            _ => status.ToString()
        };
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
