using System.Text;
using WeaponShop.Domain;
using WeaponShop.Web.Helpers;

namespace WeaponShop.Tests.Helpers;

public class OrderWordExportTests
{
    [Fact]
    public void BuildOrderDocument_EncodesCzechCharactersAsRtfUnicodeEscapes()
    {
        var order = new Order
        {
            Id = 7,
            OrderNumber = "WS-20260427-000007",
            Status = OrderStatus.Completed,
            CreatedAt = new DateTime(2026, 4, 27, 10, 30, 0, DateTimeKind.Utc),
            ContactEmail = "user@example.com",
            ContactPhone = "0999999",
            DeliveryMethod = "pickup",
            PaymentMethod = "bank-transfer",
            TotalPrice = 75000m,
            User = new Domain.Identity.ApplicationUser
            {
                FirstName = "Jiří",
                LastName = "Dvořák",
                Email = "user@example.com"
            },
            Items =
            [
                new OrderItem
                {
                    Weapon = new Weapon
                    {
                        Name = "Brokovnice"
                    },
                    Quantity = 1,
                    UnitPrice = 75000m
                }
            ]
        };

        var bytes = OrderWordExport.BuildOrderDocument(order);
        var document = Encoding.ASCII.GetString(bytes);

        Assert.StartsWith(@"{\rtf1\ansi", document);
        Assert.Contains(@"Objedn\u225?vka", document);
        Assert.Contains(@"Ji\u345?\u237?", document);
        Assert.DoesNotContain("Objedn√", document, StringComparison.Ordinal);
    }
}
