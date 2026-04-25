using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260425120000_AddPublicOrderNumber")]
public partial class AddPublicOrderNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "order_number",
            table: "purchase_requests",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "purchase_requests"
            SET "order_number" = CONCAT(
                'WS-',
                TO_CHAR("created_at_utc" AT TIME ZONE 'UTC', 'YYYYMMDD'),
                '-',
                UPPER(SUBSTRING(MD5(CONCAT("request_id"::text, '-', "created_at_utc"::text)) FROM 1 FOR 8))
            )
            WHERE "order_number" IS NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_purchase_requests_order_number",
            table: "purchase_requests",
            column: "order_number",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_purchase_requests_order_number",
            table: "purchase_requests");

        migrationBuilder.DropColumn(
            name: "order_number",
            table: "purchase_requests");
    }
}
