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
            _ => string.IsNullOrWhiteSpace(category) ? "Doplňky" : category.Trim()
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
        return "Pouze po přihlášení, ověření věku 18+ a nahrání dokladů";
    }

    public static string GetAccessoryAccessLabel()
    {
        return "Volně dostupné zboží bez věkové kontroly";
    }

    public static string GetWeaponFallbackDescription()
    {
        return "Zbraň s kontrolovaným prodejem a navazujícím schvalovacím procesem.";
    }

    public static string GetAccessoryFallbackDescription()
    {
        return "Veřejně dostupná položka e-shopu bez nutnosti věkového ověření.";
    }

    public static string GetStatusLabel(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Created => "V košíku",
            OrderStatus.AwaitingApproval => "Čeká na schválení",
            OrderStatus.Approved => "Schváleno",
            OrderStatus.Rejected => "Zamítnuto",
            OrderStatus.Completed => "Dokončeno",
            OrderStatus.AwaitingGunsmith => "Čeká na zbrojíře",
            OrderStatus.AwaitingDispatch => "Připraveno k expedici",
            OrderStatus.Shipped => "Odesláno",
            OrderStatus.ReadyForPickup => "Připraveno k vyzvednutí",
            _ => status.ToString()
        };
    }

    public static string GetAuditActionLabel(string? action)
    {
        return Normalize(action) switch
        {
            "checkoutsubmitted" => "Objednávka odeslána ke schválení",
            "publicorderautoapproved" => "Veřejná objednávka byla automaticky potvrzena",
            "adminapproved" => "Administrátor schválil objednávku",
            "publicorderconfirmed" => "Veřejná objednávka byla potvrzena",
            "warehouserejected" => "Sklad zamítl objednávku",
            "gunsmithrejected" => "Zbrojíř zamítl objednávku",
            "adminrejected" => "Administrátor zamítl objednávku",
            "warehousechecked" => "Sklad předal objednávku zbrojíři",
            "gunsmithchecked" => "Zbrojíř vrátil objednávku na sklad",
            "ordershipped" => "Objednávka byla odeslána",
            "readyforpickup" => "Objednávka je připravena k vyzvednutí",
            "pickuphandedover" => "Objednávka byla předána zákazníkovi",
            "objednávkaodeslánakeschválení" => "Objednávka odeslána ke schválení",
            "veřejnáobjednávkabylaautomatickypotvrzena" => "Veřejná objednávka byla automaticky potvrzena",
            "administrátorschválilobjednávku" => "Administrátor schválil objednávku",
            "veřejnáobjednávkabyla potvrzena" => "Veřejná objednávka byla potvrzena",
            "skladzamítlobjednávku" => "Sklad zamítl objednávku",
            "zbrojířzamítlobjednávku" => "Zbrojíř zamítl objednávku",
            "administrátorzamítlobjednávku" => "Administrátor zamítl objednávku",
            "skladpředalobjednávkuzbrojíři" => "Sklad předal objednávku zbrojíři",
            "zbrojířvrátilobjednávkunasklad" => "Zbrojíř vrátil objednávku na sklad",
            "objednávkabylaodeslána" => "Objednávka byla odeslána",
            "objednávkajepřipravenakvyzvednutí" => "Objednávka je připravena k vyzvednutí",
            "objednávkabylapředánazákazníkovi" => "Objednávka byla předána zákazníkovi",
            _ => string.IsNullOrWhiteSpace(action) ? "Bez popisu akce" : action.Trim()
        };
    }

    public static string GetActorRoleLabel(string? actorRole)
    {
        return Normalize(actorRole) switch
        {
            "admin" => "Administrátor",
            "administrátor" => "Administrátor",
            "skladnik" => "Skladník",
            "skladník" => "Skladník",
            "zbrojir" => "Zbrojíř",
            "zbrojíř" => "Zbrojíř",
            "customer" => "Zákazník",
            "zákazník" => "Zákazník",
            "unknown" => "Neznámá role",
            "neznámárole" => "Neznámá role",
            _ => string.IsNullOrWhiteSpace(actorRole) ? "Neznámá role" : actorRole.Trim()
        };
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
