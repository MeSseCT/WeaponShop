using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeaponShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesAndColumnsForClarity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Weapons_WeaponId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_UserId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Weapons",
                table: "Weapons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "Weapons",
                newName: "weapons");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "purchase_requests");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "purchase_request_items");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "user_tokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "user_roles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "user_logins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "user_claims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "roles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "role_claims");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "weapons",
                newName: "price_amount");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "weapons",
                newName: "weapon_name");

            migrationBuilder.RenameColumn(
                name: "Manufacturer",
                table: "weapons",
                newName: "manufacturer_name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "weapons",
                newName: "weapon_description");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "weapons",
                newName: "legal_category");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "weapons",
                newName: "weapon_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "purchase_requests",
                newName: "customer_user_id");

            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                table: "purchase_requests",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "purchase_requests",
                newName: "request_status");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "purchase_requests",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "purchase_requests",
                newName: "request_id");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_UserId",
                table: "purchase_requests",
                newName: "IX_purchase_requests_customer_user_id");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "purchase_request_items",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "WeaponId",
                table: "purchase_request_items",
                newName: "weapon_id");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "purchase_request_items",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "purchase_request_items",
                newName: "request_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "purchase_request_items",
                newName: "request_item_id");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_WeaponId",
                table: "purchase_request_items",
                newName: "IX_purchase_request_items_weapon_id");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderId_WeaponId",
                table: "purchase_request_items",
                newName: "IX_purchase_request_items_request_id_weapon_id");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "users",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "IdCardFileName",
                table: "users",
                newName: "id_card_file_name");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "users",
                newName: "first_name");

            migrationBuilder.RenameColumn(
                name: "DriverLicenseFileName",
                table: "users",
                newName: "driver_license_file_name");

            migrationBuilder.RenameColumn(
                name: "DocumentsUpdatedAt",
                table: "users",
                newName: "documents_updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "DateOfBirth",
                table: "users",
                newName: "date_of_birth");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "user_roles",
                newName: "IX_user_roles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "user_logins",
                newName: "IX_user_logins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "user_claims",
                newName: "IX_user_claims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "role_claims",
                newName: "IX_role_claims_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_weapons",
                table: "weapons",
                column: "weapon_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_purchase_requests",
                table: "purchase_requests",
                column: "request_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_purchase_request_items",
                table: "purchase_request_items",
                column: "request_item_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_tokens",
                table: "user_tokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_roles",
                table: "user_roles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_logins",
                table: "user_logins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_claims",
                table: "user_claims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles",
                table: "roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_role_claims",
                table: "role_claims",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_request_items_purchase_requests_request_id",
                table: "purchase_request_items",
                column: "request_id",
                principalTable: "purchase_requests",
                principalColumn: "request_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_request_items_weapons_weapon_id",
                table: "purchase_request_items",
                column: "weapon_id",
                principalTable: "weapons",
                principalColumn: "weapon_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_requests_users_customer_user_id",
                table: "purchase_requests",
                column: "customer_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_role_claims_roles_RoleId",
                table: "role_claims",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_claims_users_UserId",
                table: "user_claims",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_logins_users_UserId",
                table: "user_logins",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_roles_RoleId",
                table: "user_roles",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_users_UserId",
                table: "user_roles",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_tokens_users_UserId",
                table: "user_tokens",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_request_items_purchase_requests_request_id",
                table: "purchase_request_items");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_request_items_weapons_weapon_id",
                table: "purchase_request_items");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_requests_users_customer_user_id",
                table: "purchase_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_role_claims_roles_RoleId",
                table: "role_claims");

            migrationBuilder.DropForeignKey(
                name: "FK_user_claims_users_UserId",
                table: "user_claims");

            migrationBuilder.DropForeignKey(
                name: "FK_user_logins_users_UserId",
                table: "user_logins");

            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_roles_RoleId",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_users_UserId",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_user_tokens_users_UserId",
                table: "user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_weapons",
                table: "weapons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_tokens",
                table: "user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_roles",
                table: "user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_logins",
                table: "user_logins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_claims",
                table: "user_claims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_role_claims",
                table: "role_claims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_purchase_requests",
                table: "purchase_requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_purchase_request_items",
                table: "purchase_request_items");

            migrationBuilder.RenameTable(
                name: "weapons",
                newName: "Weapons");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "user_tokens",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "user_roles",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "user_logins",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "user_claims",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "roles",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "role_claims",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "purchase_requests",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "purchase_request_items",
                newName: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "weapon_name",
                table: "Weapons",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "weapon_description",
                table: "Weapons",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "price_amount",
                table: "Weapons",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "manufacturer_name",
                table: "Weapons",
                newName: "Manufacturer");

            migrationBuilder.RenameColumn(
                name: "legal_category",
                table: "Weapons",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "weapon_id",
                table: "Weapons",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "AspNetUsers",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "id_card_file_name",
                table: "AspNetUsers",
                newName: "IdCardFileName");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "AspNetUsers",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "driver_license_file_name",
                table: "AspNetUsers",
                newName: "DriverLicenseFileName");

            migrationBuilder.RenameColumn(
                name: "documents_updated_at_utc",
                table: "AspNetUsers",
                newName: "DocumentsUpdatedAt");

            migrationBuilder.RenameColumn(
                name: "date_of_birth",
                table: "AspNetUsers",
                newName: "DateOfBirth");

            migrationBuilder.RenameIndex(
                name: "IX_user_roles_RoleId",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_user_logins_UserId",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_user_claims_UserId",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_role_claims_RoleId",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "Orders",
                newName: "TotalPrice");

            migrationBuilder.RenameColumn(
                name: "request_status",
                table: "Orders",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "customer_user_id",
                table: "Orders",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "Orders",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "request_id",
                table: "Orders",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_requests_customer_user_id",
                table: "Orders",
                newName: "IX_Orders_UserId");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "OrderItems",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "weapon_id",
                table: "OrderItems",
                newName: "WeaponId");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                table: "OrderItems",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "request_id",
                table: "OrderItems",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "request_item_id",
                table: "OrderItems",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_request_items_weapon_id",
                table: "OrderItems",
                newName: "IX_OrderItems_WeaponId");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_request_items_request_id_weapon_id",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderId_WeaponId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Weapons",
                table: "Weapons",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Weapons_WeaponId",
                table: "OrderItems",
                column: "WeaponId",
                principalTable: "Weapons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
