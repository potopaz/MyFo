using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialBalanceToCashBoxesAndBankAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InitialBalance",
                table: "cash_boxes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InitialBalance",
                table: "bank_accounts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // For existing records, seed initial_balance from current balance
            migrationBuilder.Sql("UPDATE cash_boxes SET \"InitialBalance\" = balance;");
            migrationBuilder.Sql("UPDATE bank_accounts SET \"InitialBalance\" = balance;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialBalance",
                table: "cash_boxes");

            migrationBuilder.DropColumn(
                name: "InitialBalance",
                table: "bank_accounts");
        }
    }
}
