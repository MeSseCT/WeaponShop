using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDocumentsAndBirthDate : Migration
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
                          AND table_name = 'AspNetUsers'
                          AND column_name = 'DateOfBirth'
                    ) THEN
                        ALTER TABLE "AspNetUsers" ADD "DateOfBirth" date;
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
                          AND table_name = 'AspNetUsers'
                          AND column_name = 'DocumentsUpdatedAt'
                    ) THEN
                        ALTER TABLE "AspNetUsers" ADD "DocumentsUpdatedAt" timestamp with time zone;
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
                          AND table_name = 'AspNetUsers'
                          AND column_name = 'DriverLicenseFileName'
                    ) THEN
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'AspNetUsers'
                              AND column_name = 'GunLicenseFileName'
                        ) THEN
                            ALTER TABLE "AspNetUsers" RENAME COLUMN "GunLicenseFileName" TO "DriverLicenseFileName";
                        ELSE
                            ALTER TABLE "AspNetUsers" ADD "DriverLicenseFileName" text;
                        END IF;
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
                          AND table_name = 'AspNetUsers'
                          AND column_name = 'IdCardFileName'
                    ) THEN
                        ALTER TABLE "AspNetUsers" ADD "IdCardFileName" text;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "AspNetUsers" DROP COLUMN IF EXISTS "DateOfBirth";""");
            migrationBuilder.Sql("""ALTER TABLE "AspNetUsers" DROP COLUMN IF EXISTS "DocumentsUpdatedAt";""");
            migrationBuilder.Sql("""ALTER TABLE "AspNetUsers" DROP COLUMN IF EXISTS "DriverLicenseFileName";""");
            migrationBuilder.Sql("""ALTER TABLE "AspNetUsers" DROP COLUMN IF EXISTS "IdCardFileName";""");
        }
    }
}
