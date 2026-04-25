using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260416123000_AddWeaponImageGallery")]
public partial class AddWeaponImageGallery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "weapon_images",
            columns: table => new
            {
                weapon_image_id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                weapon_id = table.Column<int>(type: "integer", nullable: false),
                image_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weapon_images", x => x.weapon_image_id);
                table.ForeignKey(
                    name: "FK_weapon_images_weapons_weapon_id",
                    column: x => x.weapon_id,
                    principalTable: "weapons",
                    principalColumn: "weapon_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_weapon_images_weapon_id_sort_order",
            table: "weapon_images",
            columns: new[] { "weapon_id", "sort_order" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "weapon_images");
    }
}
