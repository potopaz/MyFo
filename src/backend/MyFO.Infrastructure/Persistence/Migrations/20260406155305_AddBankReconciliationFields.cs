using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBankReconciliationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_reconciled",
                schema: "txn",
                table: "transfers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_reconciled",
                schema: "txn",
                table: "movement_payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_reconciled",
                schema: "txn",
                table: "credit_card_payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_initial_balance_reconciled",
                schema: "cfg",
                table: "bank_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_reconciled",
                schema: "txn",
                table: "transfers");

            migrationBuilder.DropColumn(
                name: "is_reconciled",
                schema: "txn",
                table: "movement_payments");

            migrationBuilder.DropColumn(
                name: "is_reconciled",
                schema: "txn",
                table: "credit_card_payments");

            migrationBuilder.DropColumn(
                name: "is_initial_balance_reconciled",
                schema: "cfg",
                table: "bank_accounts");
        }
    }
}
