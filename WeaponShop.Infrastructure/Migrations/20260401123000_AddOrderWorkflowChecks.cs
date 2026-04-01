using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260401123000_AddOrderWorkflowChecks")]
    public partial class AddOrderWorkflowChecks : Migration
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
                          AND column_name = 'approved_at_utc'
                    ) THEN
                        ALTER TABLE "purchase_requests" ADD "approved_at_utc" timestamp with time zone;
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
                          AND table_name = 'purchase_requests'
                          AND column_name = 'rejected_at_utc'
                    ) THEN
                        ALTER TABLE "purchase_requests" ADD "rejected_at_utc" timestamp with time zone;
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
                          AND table_name = 'purchase_requests'
                          AND column_name = 'warehouse_checked_at_utc'
                    ) THEN
                        ALTER TABLE "purchase_requests" ADD "warehouse_checked_at_utc" timestamp with time zone;
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
                          AND table_name = 'purchase_requests'
                          AND column_name = 'gunsmith_checked_at_utc'
                    ) THEN
                        ALTER TABLE "purchase_requests" ADD "gunsmith_checked_at_utc" timestamp with time zone;
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
                          AND table_name = 'purchase_requests'
                          AND column_name = 'warehouse_prepared_at_utc'
                    ) THEN
                        ALTER TABLE "purchase_requests" ADD "warehouse_prepared_at_utc" timestamp with time zone;
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
                          AND table_name = 'purchase_requests'
                          AND column_name = 'shipped_at_utc'
                    ) THEN
                        ALTER TABLE "purchase_requests" ADD "shipped_at_utc" timestamp with time zone;
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
                          AND table_name = 'purchase_requests'
                          AND column_name = 'ready_for_pickup_at_utc'
                    ) THEN
                        ALTER TABLE "purchase_requests" ADD "ready_for_pickup_at_utc" timestamp with time zone;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "approved_at_utc";""");
            migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "rejected_at_utc";""");
            migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "warehouse_checked_at_utc";""");
            migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "gunsmith_checked_at_utc";""");
            migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "warehouse_prepared_at_utc";""");
            migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "shipped_at_utc";""");
            migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "ready_for_pickup_at_utc";""");
        }
    }
}
