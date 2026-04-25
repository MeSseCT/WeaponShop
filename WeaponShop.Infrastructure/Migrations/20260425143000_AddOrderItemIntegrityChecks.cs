using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260425143000_AddOrderItemIntegrityChecks")]
public partial class AddOrderItemIntegrityChecks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "CK_purchase_request_items_exactly_one_catalog_item",
            table: "purchase_request_items",
            sql:
            """
            (
                (CASE WHEN weapon_id IS NULL THEN 0 ELSE 1 END) +
                (CASE WHEN accessory_id IS NULL THEN 0 ELSE 1 END)
            ) = 1
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_purchase_request_items_quantity_positive",
            table: "purchase_request_items",
            sql: "quantity > 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_purchase_request_items_exactly_one_catalog_item",
            table: "purchase_request_items");

        migrationBuilder.DropCheckConstraint(
            name: "CK_purchase_request_items_quantity_positive",
            table: "purchase_request_items");
    }
}
