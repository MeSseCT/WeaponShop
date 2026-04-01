using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260401134500_AddPickupHandedOver")]
    public partial class AddPickupHandedOver : Migration
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
                          AND table_name = 'purchase_requests'
                          AND column_name = 'pickup_handed_over_at_utc'
                    ) THEN
                        ALTER TABLE "purchase_requests" ADD "pickup_handed_over_at_utc" timestamp with time zone;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "pickup_handed_over_at_utc";""");
        }
    }
}
