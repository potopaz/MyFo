using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameExchangeRateToPrimaryExchangeRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "exchange_rate",
                table: "movements",
                newName: "primary_exchange_rate");

            migrationBuilder.AddColumn<decimal>(
                name: "primary_exchange_rate",
                table: "transfers",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                defaultValue: 1m);

            // Data migration: existing exchange_rate was the primary rate; compute new exchange_rate = amountTo/amount
            migrationBuilder.Sql(
                "UPDATE transfers SET primary_exchange_rate = exchange_rate;");
            migrationBuilder.Sql(
                "UPDATE transfers SET exchange_rate = CASE WHEN amount > 0 THEN ROUND(amount_to::numeric / amount, 8) ELSE 1 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "primary_exchange_rate",
                table: "transfers");

            migrationBuilder.RenameColumn(
                name: "primary_exchange_rate",
                table: "movements",
                newName: "exchange_rate");
        }
    }
}
