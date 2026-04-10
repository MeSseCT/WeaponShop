using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    public partial class AddMixedCatalogCheckout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "accessory_id",
                table: "purchase_request_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_city",
                table: "purchase_requests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "billing_name",
                table: "purchase_requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "billing_postal_code",
                table: "purchase_requests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "billing_street",
                table: "purchase_requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "purchase_requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                table: "purchase_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "customer_note",
                table: "purchase_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "delivery_method",
                table: "purchase_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "purchase_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_city",
                table: "purchase_requests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_name",
                table: "purchase_requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_postal_code",
                table: "purchase_requests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_street",
                table: "purchase_requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropIndex(
                name: "IX_purchase_request_items_request_id_weapon_id",
                table: "purchase_request_items");

            migrationBuilder.AlterColumn<int>(
                name: "weapon_id",
                table: "purchase_request_items",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_request_items_accessory_id",
                table: "purchase_request_items",
                column: "accessory_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_request_items_request_id_accessory_id",
                table: "purchase_request_items",
                columns: new[] { "request_id", "accessory_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_request_items_request_id_weapon_id",
                table: "purchase_request_items",
                columns: new[] { "request_id", "weapon_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_request_items_catalog_accessories_accessory_id",
                table: "purchase_request_items",
                column: "accessory_id",
                principalTable: "catalog_accessories",
                principalColumn: "accessory_id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_request_items_catalog_accessories_accessory_id",
                table: "purchase_request_items");

            migrationBuilder.DropIndex(
                name: "IX_purchase_request_items_accessory_id",
                table: "purchase_request_items");

            migrationBuilder.DropIndex(
                name: "IX_purchase_request_items_request_id_accessory_id",
                table: "purchase_request_items");

            migrationBuilder.DropIndex(
                name: "IX_purchase_request_items_request_id_weapon_id",
                table: "purchase_request_items");

            migrationBuilder.DropColumn(
                name: "accessory_id",
                table: "purchase_request_items");

            migrationBuilder.DropColumn(
                name: "billing_city",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "billing_name",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "billing_postal_code",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "billing_street",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "customer_note",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "delivery_method",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "shipping_city",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "shipping_name",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "shipping_postal_code",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "shipping_street",
                table: "purchase_requests");

            migrationBuilder.AlterColumn<int>(
                name: "weapon_id",
                table: "purchase_request_items",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_request_items_request_id_weapon_id",
                table: "purchase_request_items",
                columns: new[] { "request_id", "weapon_id" },
                unique: true);
        }
    }
}
