using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizeSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.EnsureSchema(
                name: "cfg");

            migrationBuilder.EnsureSchema(
                name: "txn");

            migrationBuilder.EnsureSchema(
                name: "cmn");

            migrationBuilder.RenameTable(
                name: "transfers",
                newName: "transfers",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "subcategories",
                newName: "subcategories",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "statement_periods",
                newName: "statement_periods",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "statement_payments",
                newName: "statement_payments",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "statement_payment_allocations",
                newName: "statement_payment_allocations",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "statement_line_items",
                newName: "statement_line_items",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "movements",
                newName: "movements",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "movement_payments",
                newName: "movement_payments",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "frequent_movements",
                newName: "frequent_movements",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "family_members",
                newName: "family_members",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "family_invitations",
                newName: "family_invitations",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "family_currencies",
                newName: "family_currencies",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "family_admin_configs",
                newName: "family_admin_configs",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "families",
                newName: "families",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "exchange_rate_snapshots",
                newName: "exchange_rate_snapshots",
                newSchema: "cmn");

            migrationBuilder.RenameTable(
                name: "currencies",
                newName: "currencies",
                newSchema: "cmn");

            migrationBuilder.RenameTable(
                name: "credit_cards",
                newName: "credit_cards",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "credit_card_payments",
                newName: "credit_card_payments",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "credit_card_members",
                newName: "credit_card_members",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "credit_card_installments",
                newName: "credit_card_installments",
                newSchema: "txn");

            migrationBuilder.RenameTable(
                name: "cost_centers",
                newName: "cost_centers",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "categories",
                newName: "categories",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "cash_boxes",
                newName: "cash_boxes",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "cash_box_permissions",
                newName: "cash_box_permissions",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "bank_accounts",
                newName: "bank_accounts",
                newSchema: "cfg");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "AspNetUsers",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "transfers",
                schema: "txn",
                newName: "transfers");

            migrationBuilder.RenameTable(
                name: "subcategories",
                schema: "cfg",
                newName: "subcategories");

            migrationBuilder.RenameTable(
                name: "statement_periods",
                schema: "txn",
                newName: "statement_periods");

            migrationBuilder.RenameTable(
                name: "statement_payments",
                schema: "txn",
                newName: "statement_payments");

            migrationBuilder.RenameTable(
                name: "statement_payment_allocations",
                schema: "txn",
                newName: "statement_payment_allocations");

            migrationBuilder.RenameTable(
                name: "statement_line_items",
                schema: "txn",
                newName: "statement_line_items");

            migrationBuilder.RenameTable(
                name: "movements",
                schema: "txn",
                newName: "movements");

            migrationBuilder.RenameTable(
                name: "movement_payments",
                schema: "txn",
                newName: "movement_payments");

            migrationBuilder.RenameTable(
                name: "frequent_movements",
                schema: "txn",
                newName: "frequent_movements");

            migrationBuilder.RenameTable(
                name: "family_members",
                schema: "cfg",
                newName: "family_members");

            migrationBuilder.RenameTable(
                name: "family_invitations",
                schema: "cfg",
                newName: "family_invitations");

            migrationBuilder.RenameTable(
                name: "family_currencies",
                schema: "cfg",
                newName: "family_currencies");

            migrationBuilder.RenameTable(
                name: "family_admin_configs",
                schema: "cfg",
                newName: "family_admin_configs");

            migrationBuilder.RenameTable(
                name: "families",
                schema: "cfg",
                newName: "families");

            migrationBuilder.RenameTable(
                name: "exchange_rate_snapshots",
                schema: "cmn",
                newName: "exchange_rate_snapshots");

            migrationBuilder.RenameTable(
                name: "currencies",
                schema: "cmn",
                newName: "currencies");

            migrationBuilder.RenameTable(
                name: "credit_cards",
                schema: "cfg",
                newName: "credit_cards");

            migrationBuilder.RenameTable(
                name: "credit_card_payments",
                schema: "txn",
                newName: "credit_card_payments");

            migrationBuilder.RenameTable(
                name: "credit_card_members",
                schema: "cfg",
                newName: "credit_card_members");

            migrationBuilder.RenameTable(
                name: "credit_card_installments",
                schema: "txn",
                newName: "credit_card_installments");

            migrationBuilder.RenameTable(
                name: "cost_centers",
                schema: "cfg",
                newName: "cost_centers");

            migrationBuilder.RenameTable(
                name: "categories",
                schema: "cfg",
                newName: "categories");

            migrationBuilder.RenameTable(
                name: "cash_boxes",
                schema: "cfg",
                newName: "cash_boxes");

            migrationBuilder.RenameTable(
                name: "cash_box_permissions",
                schema: "cfg",
                newName: "cash_box_permissions");

            migrationBuilder.RenameTable(
                name: "bank_accounts",
                schema: "cfg",
                newName: "bank_accounts");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "public",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                schema: "public",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                schema: "public",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "public",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "public",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "public",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "public",
                newName: "AspNetRoleClaims");
        }
    }
}
