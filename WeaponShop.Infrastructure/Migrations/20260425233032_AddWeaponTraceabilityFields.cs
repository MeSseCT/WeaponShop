using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaponTraceabilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "caliber_value",
                table: "weapons",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "primary_serial_number",
                table: "weapons",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "type_designation",
                table: "weapons",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "weapon_parts",
                columns: table => new
                {
                    weapon_part_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    weapon_id = table.Column<int>(type: "integer", nullable: false),
                    assembly_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    part_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapon_parts", x => x.weapon_part_id);
                    table.ForeignKey(
                        name: "FK_weapon_parts_weapons_weapon_id",
                        column: x => x.weapon_id,
                        principalTable: "weapons",
                        principalColumn: "weapon_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weapon_parts_serial_number",
                table: "weapon_parts",
                column: "serial_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weapon_parts_weapon_id_assembly_label",
                table: "weapon_parts",
                columns: new[] { "weapon_id", "assembly_label" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weapon_parts");

            migrationBuilder.DropColumn(
                name: "caliber_value",
                table: "weapons");

            migrationBuilder.DropColumn(
                name: "primary_serial_number",
                table: "weapons");

            migrationBuilder.DropColumn(
                name: "type_designation",
                table: "weapons");
        }
    }
}
