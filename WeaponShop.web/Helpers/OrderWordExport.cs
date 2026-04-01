using System.Text;
using WeaponShop.Domain;

namespace WeaponShop.Web.Helpers;

public static class OrderWordExport
{
    public static byte[] BuildOrderDocument(Order order)
    {
        var sb = new StringBuilder();
        sb.Append(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Calibri;}}\fs24");

        AppendLine(sb, $"Order #{order.Id}");
        AppendLine(sb, $"Status: {order.Status}");
        AppendLine(sb, $"Created: {order.CreatedAt.ToLocalTime():g}");
        AppendLine(sb, $"Customer: {order.User?.FirstName} {order.User?.LastName} ({order.User?.Email})");
        AppendLine(sb, $"Total: {order.TotalPrice:C}");
        sb.Append(@"\par ");

        AppendLine(sb, "Items:");
        foreach (var item in order.Items)
        {
            var name = item.Weapon?.Name ?? "Unknown item";
            var lineTotal = item.UnitPrice * item.Quantity;
            AppendLine(sb, $"- {name} x {item.Quantity} @ {item.UnitPrice:C} = {lineTotal:C}");
        }

        sb.Append(@"\par ");
        AppendLine(sb, "Audit:");
        foreach (var audit in order.Audits.OrderByDescending(a => a.OccurredAtUtc))
        {
            var when = audit.OccurredAtUtc.ToLocalTime().ToString("g");
            AppendLine(sb, $"{when} - {audit.ActorRole} {audit.ActorName}: {audit.Action} ({audit.FromStatus} -> {audit.ToStatus})");
            if (!string.IsNullOrWhiteSpace(audit.Notes))
            {
                AppendLine(sb, $"Notes: {audit.Notes}");
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
}
