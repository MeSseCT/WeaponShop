using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260401140000_AddWeaponStockAvailability")]
    public partial class AddWeaponStockAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'weapons'
                          AND column_name = 'stock_quantity'
                    ) THEN
                        ALTER TABLE "weapons" ADD "stock_quantity" integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'weapons'
                          AND column_name = 'is_available'
                    ) THEN
                        ALTER TABLE "weapons" ADD "is_available" boolean NOT NULL DEFAULT true;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "weapons" DROP COLUMN IF EXISTS "stock_quantity";""");
            migrationBuilder.Sql("""ALTER TABLE "weapons" DROP COLUMN IF EXISTS "is_available";""");
        }
    }
}
