using System.Text;
using WeaponShop.Domain;

namespace WeaponShop.Web.Helpers;

public static class OrderWordExport
{
    public static byte[] BuildOrderDocument(Order order)
    {
        var sb = new StringBuilder();
        sb.Append(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Calibri;}}\fs24");

        AppendLine(sb, $"Objednavka #{order.Id}");
        AppendLine(sb, $"Stav: {CatalogPresentation.GetStatusLabel(order.Status)}");
        AppendLine(sb, $"Vytvoreno: {order.CreatedAt.ToLocalTime():g}");
        AppendLine(sb, $"Zakaznik: {order.User?.FirstName} {order.User?.LastName} ({order.User?.Email})");
        AppendLine(sb, $"Kontakt: {order.ContactEmail} / {order.ContactPhone}");
        AppendLine(sb, $"Doruceni: {(order.DeliveryMethod == "shipping" ? "Doruceni na adresu" : "Osobni odber")}");
        AppendLine(sb, $"Platba: {ResolvePaymentLabel(order.PaymentMethod)}");
        AppendLine(sb, $"Celkem: {order.TotalPrice:C}");
        sb.Append(@"\par ");

        AppendLine(sb, "Polozky:");
        foreach (var item in order.Items)
        {
            var name = item.GetDisplayName();
            var lineTotal = item.UnitPrice * item.Quantity;
            AppendLine(sb, $"- {name} x {item.Quantity} @ {item.UnitPrice:C} = {lineTotal:C}");
        }

        sb.Append(@"\par ");
        AppendLine(sb, "Historie:");
        foreach (var audit in order.Audits.OrderByDescending(a => a.OccurredAtUtc))
        {
            var when = audit.OccurredAtUtc.ToLocalTime().ToString("g");
            AppendLine(sb, $"{when} - {audit.ActorRole} {audit.ActorName}: {audit.Action} ({audit.FromStatus} -> {audit.ToStatus})");
            if (!string.IsNullOrWhiteSpace(audit.Notes))
            {
                AppendLine(sb, $"Poznamka: {audit.Notes}");
            }
        }

        sb.Append("}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void AppendLine(StringBuilder sb, string text)
    {
        sb.Append(Escape(text ?? string.Empty));
        sb.Append(@"\par ");
    }

    private static string Escape(string text)
    {
        return text
            .Replace(@"\", @"\\")
            .Replace("{", @"\{")
            .Replace("}", @"\}");
    }

    private static string ResolvePaymentLabel(string paymentMethod)
    {
        return paymentMethod switch
        {
            "cash-on-delivery" => "Dobirka",
            "cash-on-pickup" => "Platba pri prevzeti",
            _ => "Bankovni prevod"
        };
    }
}
