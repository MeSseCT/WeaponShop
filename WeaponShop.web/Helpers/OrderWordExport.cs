using System.Text;
using WeaponShop.Domain;

namespace WeaponShop.Web.Helpers;

public static class OrderWordExport
{
    public static byte[] BuildOrderDocument(Order order)
    {
        var sb = new StringBuilder();
        sb.Append(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Calibri;}}\fs24");

        AppendLine(sb, $"Objednávka {order.GetPublicOrderNumber()}");
        AppendLine(sb, $"Stav: {CatalogPresentation.GetStatusLabel(order.Status)}");
        AppendLine(sb, $"Vytvořeno: {order.CreatedAt.ToLocalTime():g}");
        AppendLine(sb, $"Zákazník: {order.User?.FirstName} {order.User?.LastName} ({order.User?.Email})");
        AppendLine(sb, $"Kontakt: {order.ContactEmail} / {order.ContactPhone}");
        AppendLine(sb, $"Doručení: {(order.DeliveryMethod == "shipping" ? "Doručení na adresu" : "Osobní odběr")}");
        AppendLine(sb, $"Platba: {ResolvePaymentLabel(order.PaymentMethod)}");
        AppendLine(sb, $"Celkem: {order.TotalPrice:C}");
        sb.Append(@"\par ");

        AppendLine(sb, "Položky:");
        foreach (var item in order.Items)
        {
            var name = item.GetDisplayName();
            var lineTotal = item.UnitPrice * item.Quantity;
            AppendLine(sb, $"- {name} x {item.Quantity} @ {item.UnitPrice:C} = {lineTotal:C}");

            if (item.IsWeapon && item.Weapon is not null)
            {
                foreach (var unit in item.Weapon.Units
                             .Where(unit => unit.ReservedOrderId == order.Id || unit.SoldOrderId == order.Id)
                             .OrderBy(unit => unit.PrimarySerialNumber))
                {
                    AppendLine(sb, $"  Výrobní číslo: {unit.PrimarySerialNumber}");
                    foreach (var part in unit.Parts.OrderBy(part => part.SlotNumber))
                    {
                        AppendLine(sb, $"    {part.PartName}: {part.SerialNumber}");
                    }
                }
            }
        }

        sb.Append(@"\par ");
        AppendLine(sb, "Historie:");
        foreach (var audit in order.Audits.OrderByDescending(a => a.OccurredAtUtc))
        {
            var when = audit.OccurredAtUtc.ToLocalTime().ToString("g");
            AppendLine(
                sb,
                $"{when} - {CatalogPresentation.GetActorRoleLabel(audit.ActorRole)} {audit.ActorName}: {CatalogPresentation.GetAuditActionLabel(audit.Action)} ({CatalogPresentation.GetStatusLabel(audit.FromStatus)} -> {CatalogPresentation.GetStatusLabel(audit.ToStatus)})");
            if (!string.IsNullOrWhiteSpace(audit.Notes))
            {
                AppendLine(sb, $"Poznámka: {audit.Notes}");
            }
        }

        sb.Append("}");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static void AppendLine(StringBuilder sb, string text)
    {
        sb.Append(Escape(text ?? string.Empty));
        sb.Append(@"\par ");
    }

    private static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            switch (character)
            {
                case '\\':
                    sb.Append(@"\\");
                    break;
                case '{':
                    sb.Append(@"\{");
                    break;
                case '}':
                    sb.Append(@"\}");
                    break;
                default:
                    if (character <= sbyte.MaxValue)
                    {
                        sb.Append(character);
                    }
                    else
                    {
                        sb.Append(@"\u");
                        sb.Append((short)character);
                        sb.Append('?');
                    }

                    break;
            }
        }

        return sb.ToString();
    }

    private static string ResolvePaymentLabel(string paymentMethod)
    {
        return paymentMethod switch
        {
            "cash-on-delivery" => "Dobírka",
            "cash-on-pickup" => "Platba při převzetí",
            _ => "Bankovní převod"
        };
    }
}
