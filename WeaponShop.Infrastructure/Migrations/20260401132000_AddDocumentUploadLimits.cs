using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeaponShop.Infrastructure;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260401132000_AddDocumentUploadLimits")]
    public partial class AddDocumentUploadLimits : Migration
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
                          AND table_name = 'users'
                          AND column_name = 'documents_upload_window_start_utc'
                    ) THEN
                        ALTER TABLE "users" ADD "documents_upload_window_start_utc" timestamp with time zone;
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
                          AND table_name = 'users'
                          AND column_name = 'documents_upload_count'
                    ) THEN
                        ALTER TABLE "users" ADD "documents_upload_count" integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "users" DROP COLUMN IF EXISTS "documents_upload_window_start_utc";""");
            migrationBuilder.Sql("""ALTER TABLE "users" DROP COLUMN IF EXISTS "documents_upload_count";""");
        }
    }
}
