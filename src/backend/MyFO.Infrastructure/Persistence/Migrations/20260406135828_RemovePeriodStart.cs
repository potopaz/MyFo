using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePeriodStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "period_start",
                schema: "txn",
                table: "statement_periods");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "period_start",
                schema: "txn",
                table: "statement_periods",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
