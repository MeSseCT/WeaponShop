using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260416131500_AddAccessoryImageGallery")]
public partial class AddAccessoryImageGallery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "accessory_images",
            columns: table => new
            {
                accessory_image_id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                accessory_id = table.Column<int>(type: "integer", nullable: false),
                image_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_accessory_images", x => x.accessory_image_id);
                table.ForeignKey(
                    name: "FK_accessory_images_catalog_accessories_accessory_id",
                    column: x => x.accessory_id,
                    principalTable: "catalog_accessories",
                    principalColumn: "accessory_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_accessory_images_accessory_id_sort_order",
            table: "accessory_images",
            columns: new[] { "accessory_id", "sort_order" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "accessory_images");
    }
}
