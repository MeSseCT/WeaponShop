using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260410183000_RepairMixedCatalogCheckoutSchema")]
public partial class RepairMixedCatalogCheckoutSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "purchase_request_items"
            ADD COLUMN IF NOT EXISTS "accessory_id" integer;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "billing_city" character varying(100) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "billing_name" character varying(200) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "billing_postal_code" character varying(20) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "billing_street" character varying(200) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "contact_email" character varying(200) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "contact_phone" character varying(50) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "customer_note" character varying(1000) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "delivery_method" character varying(50) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "payment_method" character varying(50) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "shipping_city" character varying(100) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "shipping_name" character varying(200) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "shipping_postal_code" character varying(20) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_requests"
            ADD COLUMN IF NOT EXISTS "shipping_street" character varying(200) NOT NULL DEFAULT '';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "purchase_request_items"
            ALTER COLUMN "weapon_id" DROP NOT NULL;
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_purchase_request_items_accessory_id"
            ON "purchase_request_items" ("accessory_id");
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_purchase_request_items_request_id_accessory_id"
            ON "purchase_request_items" ("request_id", "accessory_id");
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_purchase_request_items_request_id_weapon_id"
            ON "purchase_request_items" ("request_id", "weapon_id");
            """);

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM pg_constraint
                    WHERE conname = 'FK_purchase_request_items_catalog_accessories_accessory_id'
                ) THEN
                    ALTER TABLE "purchase_request_items"
                    ADD CONSTRAINT "FK_purchase_request_items_catalog_accessories_accessory_id"
                    FOREIGN KEY ("accessory_id")
                    REFERENCES "catalog_accessories" ("accessory_id")
                    ON DELETE RESTRICT;
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "purchase_request_items"
            DROP CONSTRAINT IF EXISTS "FK_purchase_request_items_catalog_accessories_accessory_id";
            """);

        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_purchase_request_items_accessory_id";""");
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_purchase_request_items_request_id_accessory_id";""");
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_purchase_request_items_request_id_weapon_id";""");

        migrationBuilder.Sql("""ALTER TABLE "purchase_request_items" DROP COLUMN IF EXISTS "accessory_id";""");

        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "billing_city";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "billing_name";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "billing_postal_code";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "billing_street";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "contact_email";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "contact_phone";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "customer_note";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "delivery_method";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "payment_method";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "shipping_city";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "shipping_name";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "shipping_postal_code";""");
        migrationBuilder.Sql("""ALTER TABLE "purchase_requests" DROP COLUMN IF EXISTS "shipping_street";""");
    }
}
