using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260409120000_AddPublicAccessoriesCatalog")]
    public partial class AddPublicAccessoriesCatalog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_accessories",
                columns: table => new
                {
                    accessory_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accessory_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    catalog_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    accessory_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    stock_quantity = table.Column<int>(type: "integer", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    manufacturer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_accessories", x => x.accessory_id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_accessories");
        }
    }
}
