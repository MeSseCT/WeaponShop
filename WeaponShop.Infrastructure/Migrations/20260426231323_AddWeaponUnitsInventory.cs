using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaponUnitsInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weapon_units",
                columns: table => new
                {
                    weapon_unit_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    weapon_id = table.Column<int>(type: "integer", nullable: false),
                    primary_serial_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    unit_status = table.Column<int>(type: "integer", nullable: false),
                    reserved_order_id = table.Column<int>(type: "integer", nullable: true),
                    sold_order_id = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapon_units", x => x.weapon_unit_id);
                    table.ForeignKey(
                        name: "FK_weapon_units_purchase_requests_reserved_order_id",
                        column: x => x.reserved_order_id,
                        principalTable: "purchase_requests",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_weapon_units_purchase_requests_sold_order_id",
                        column: x => x.sold_order_id,
                        principalTable: "purchase_requests",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_weapon_units_weapons_weapon_id",
                        column: x => x.weapon_id,
                        principalTable: "weapons",
                        principalColumn: "weapon_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weapon_unit_parts",
                columns: table => new
                {
                    weapon_unit_part_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    weapon_unit_id = table.Column<int>(type: "integer", nullable: false),
                    slot_number = table.Column<int>(type: "integer", nullable: false),
                    part_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapon_unit_parts", x => x.weapon_unit_part_id);
                    table.ForeignKey(
                        name: "FK_weapon_unit_parts_weapon_units_weapon_unit_id",
                        column: x => x.weapon_unit_id,
                        principalTable: "weapon_units",
                        principalColumn: "weapon_unit_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weapon_unit_parts_serial_number",
                table: "weapon_unit_parts",
                column: "serial_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weapon_unit_parts_weapon_unit_id_slot_number",
                table: "weapon_unit_parts",
                columns: new[] { "weapon_unit_id", "slot_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weapon_units_primary_serial_number",
                table: "weapon_units",
                column: "primary_serial_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weapon_units_reserved_order_id",
                table: "weapon_units",
                column: "reserved_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_weapon_units_sold_order_id",
                table: "weapon_units",
                column: "sold_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_weapon_units_weapon_id_unit_status",
                table: "weapon_units",
                columns: new[] { "weapon_id", "unit_status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weapon_unit_parts");

            migrationBuilder.DropTable(
                name: "weapon_units");
        }
    }
}
