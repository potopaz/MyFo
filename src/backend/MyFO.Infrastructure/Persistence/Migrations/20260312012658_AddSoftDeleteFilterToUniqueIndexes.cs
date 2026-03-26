using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteFilterToUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subcategories_family_category_name",
                table: "subcategories");

            migrationBuilder.DropIndex(
                name: "ix_family_currencies_family_currency",
                table: "family_currencies");

            migrationBuilder.DropIndex(
                name: "ix_credit_cards_family_name",
                table: "credit_cards");

            migrationBuilder.DropIndex(
                name: "ix_cost_centers_family_name",
                table: "cost_centers");

            migrationBuilder.DropIndex(
                name: "ix_categories_family_name",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_cash_boxes_family_name",
                table: "cash_boxes");

            migrationBuilder.DropIndex(
                name: "ix_bank_accounts_family_name",
                table: "bank_accounts");

            migrationBuilder.CreateIndex(
                name: "ix_subcategories_family_category_name",
                table: "subcategories",
                columns: new[] { "family_id", "category_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_family_currencies_family_currency",
                table: "family_currencies",
                columns: new[] { "family_id", "currency_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_credit_cards_family_name",
                table: "credit_cards",
                columns: new[] { "family_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_family_name",
                table: "cost_centers",
                columns: new[] { "family_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_categories_family_name",
                table: "categories",
                columns: new[] { "family_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cash_boxes_family_name",
                table: "cash_boxes",
                columns: new[] { "family_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_family_name",
                table: "bank_accounts",
                columns: new[] { "family_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subcategories_family_category_name",
                table: "subcategories");

            migrationBuilder.DropIndex(
                name: "ix_family_currencies_family_currency",
                table: "family_currencies");

            migrationBuilder.DropIndex(
                name: "ix_credit_cards_family_name",
                table: "credit_cards");

            migrationBuilder.DropIndex(
                name: "ix_cost_centers_family_name",
                table: "cost_centers");

            migrationBuilder.DropIndex(
                name: "ix_categories_family_name",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_cash_boxes_family_name",
                table: "cash_boxes");

            migrationBuilder.DropIndex(
                name: "ix_bank_accounts_family_name",
                table: "bank_accounts");

            migrationBuilder.CreateIndex(
                name: "ix_subcategories_family_category_name",
                table: "subcategories",
                columns: new[] { "family_id", "category_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_family_currencies_family_currency",
                table: "family_currencies",
                columns: new[] { "family_id", "currency_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credit_cards_family_name",
                table: "credit_cards",
                columns: new[] { "family_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_family_name",
                table: "cost_centers",
                columns: new[] { "family_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_family_name",
                table: "categories",
                columns: new[] { "family_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cash_boxes_family_name",
                table: "cash_boxes",
                columns: new[] { "family_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_family_name",
                table: "bank_accounts",
                columns: new[] { "family_id", "name" },
                unique: true);
        }
    }
}
