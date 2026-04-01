using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260401154000_AddNotificationOrderLink")]
    public partial class AddNotificationOrderLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "user_notifications"
                ADD COLUMN IF NOT EXISTS "request_id" integer;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'fk_user_notifications_order'
                    ) THEN
                        ALTER TABLE "user_notifications"
                            ADD CONSTRAINT "fk_user_notifications_order"
                            FOREIGN KEY ("request_id")
                            REFERENCES "purchase_requests" ("request_id")
                            ON DELETE CASCADE;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'public'
                          AND indexname = 'ix_user_notifications_request_id_created_at_utc'
                    ) THEN
                        CREATE INDEX "ix_user_notifications_request_id_created_at_utc"
                            ON "user_notifications" ("request_id", "created_at_utc");
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'fk_user_notifications_order'
                    ) THEN
                        ALTER TABLE "user_notifications"
                            DROP CONSTRAINT "fk_user_notifications_order";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_indexes
                        WHERE schemaname = 'public'
                          AND indexname = 'ix_user_notifications_request_id_created_at_utc'
                    ) THEN
                        DROP INDEX "ix_user_notifications_request_id_created_at_utc";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "user_notifications"
                DROP COLUMN IF EXISTS "request_id";
                """);
        }
    }
}
