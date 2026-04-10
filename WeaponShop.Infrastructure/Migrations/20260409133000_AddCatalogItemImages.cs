using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260409133000_AddCatalogItemImages")]
    public partial class AddCatalogItemImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_file_name",
                table: "weapons",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_file_name",
                table: "catalog_accessories",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_file_name",
                table: "weapons");

            migrationBuilder.DropColumn(
                name: "image_file_name",
                table: "catalog_accessories");
        }
    }
}
