using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaponCategoryVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_purchase_request_items_exactly_one_catalog_item",
                table: "purchase_request_items");

            migrationBuilder.AddColumn<bool>(
                name: "firearms_license_recorded",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "id_card_issued_in_czech_republic",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchase_request_items_exactly_one_catalog_item",
                table: "purchase_request_items",
                sql: "(\n    (CASE WHEN weapon_id IS NULL THEN 0 ELSE 1 END) +\n    (CASE WHEN accessory_id IS NULL THEN 0 ELSE 1 END)\n) = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_purchase_request_items_exactly_one_catalog_item",
                table: "purchase_request_items");

            migrationBuilder.DropColumn(
                name: "firearms_license_recorded",
                table: "users");

            migrationBuilder.DropColumn(
                name: "id_card_issued_in_czech_republic",
                table: "users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchase_request_items_exactly_one_catalog_item",
                table: "purchase_request_items",
                sql: "\n(\n    (CASE WHEN weapon_id IS NULL THEN 0 ELSE 1 END) +\n    (CASE WHEN accessory_id IS NULL THEN 0 ELSE 1 END)\n) = 1");
        }
    }
}
