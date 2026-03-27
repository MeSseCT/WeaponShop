using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVerificationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DocumentsUpdatedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GunLicenseFileName",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdCardFileName",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentsUpdatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GunLicenseFileName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IdCardFileName",
                table: "AspNetUsers");
        }
    }
}
