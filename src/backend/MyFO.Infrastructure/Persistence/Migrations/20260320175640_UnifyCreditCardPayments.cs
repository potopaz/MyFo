using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnifyCreditCardPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_statement_payment_allocations_statement_payments_family_id_~",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropTable(
                name: "statement_payments",
                schema: "txn");

            migrationBuilder.DropIndex(
                name: "ix_statement_periods_one_open",
                schema: "txn",
                table: "statement_periods");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "txn",
                table: "statement_periods",
                newName: "payment_status");

            migrationBuilder.RenameColumn(
                name: "statement_payment_id",
                schema: "txn",
                table: "statement_payment_allocations",
                newName: "credit_card_payment_id");

            migrationBuilder.RenameColumn(
                name: "IsTotalPayment",
                schema: "txn",
                table: "credit_card_payments",
                newName: "is_total_payment");

            migrationBuilder.AddColumn<Guid>(
                name: "statement_period_id",
                schema: "txn",
                table: "credit_card_payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_statement_periods_one_open",
                schema: "txn",
                table: "statement_periods",
                columns: new[] { "family_id", "credit_card_id" },
                unique: true,
                filter: "closed_at IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_credit_card_payments_period",
                schema: "txn",
                table: "credit_card_payments",
                columns: new[] { "family_id", "statement_period_id" },
                filter: "statement_period_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_credit_card_payments_statement_periods_family_id_statement_~",
                schema: "txn",
                table: "credit_card_payments",
                columns: new[] { "family_id", "statement_period_id" },
                principalSchema: "txn",
                principalTable: "statement_periods",
                principalColumns: new[] { "family_id", "statement_period_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_statement_payment_allocations_credit_card_payments_family_i~",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "credit_card_payment_id" },
                principalSchema: "txn",
                principalTable: "credit_card_payments",
                principalColumns: new[] { "family_id", "credit_card_payment_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_credit_card_payments_statement_periods_family_id_statement_~",
                schema: "txn",
                table: "credit_card_payments");

            migrationBuilder.DropForeignKey(
                name: "FK_statement_payment_allocations_credit_card_payments_family_i~",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropIndex(
                name: "ix_statement_periods_one_open",
                schema: "txn",
                table: "statement_periods");

            migrationBuilder.DropIndex(
                name: "ix_credit_card_payments_period",
                schema: "txn",
                table: "credit_card_payments");

            migrationBuilder.DropColumn(
                name: "statement_period_id",
                schema: "txn",
                table: "credit_card_payments");

            migrationBuilder.RenameColumn(
                name: "payment_status",
                schema: "txn",
                table: "statement_periods",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "credit_card_payment_id",
                schema: "txn",
                table: "statement_payment_allocations",
                newName: "statement_payment_id");

            migrationBuilder.RenameColumn(
                name: "is_total_payment",
                schema: "txn",
                table: "credit_card_payments",
                newName: "IsTotalPayment");

            migrationBuilder.CreateTable(
                name: "statement_payments",
                schema: "txn",
                columns: table => new
                {
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_in_primary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_in_secondary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bank_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_box_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_total_payment = table.Column<bool>(type: "boolean", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    primary_exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    secondary_exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statement_payments", x => new { x.family_id, x.statement_payment_id });
                    table.ForeignKey(
                        name: "FK_statement_payments_statement_periods_family_id_statement_pe~",
                        columns: x => new { x.family_id, x.statement_period_id },
                        principalSchema: "txn",
                        principalTable: "statement_periods",
                        principalColumns: new[] { "family_id", "statement_period_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_statement_periods_one_open",
                schema: "txn",
                table: "statement_periods",
                columns: new[] { "family_id", "credit_card_id" },
                unique: true,
                filter: "status = 'Open' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_statement_payments_period",
                schema: "txn",
                table: "statement_payments",
                columns: new[] { "family_id", "statement_period_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_statement_payment_allocations_statement_payments_family_id_~",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "statement_payment_id" },
                principalSchema: "txn",
                principalTable: "statement_payments",
                principalColumns: new[] { "family_id", "statement_payment_id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
