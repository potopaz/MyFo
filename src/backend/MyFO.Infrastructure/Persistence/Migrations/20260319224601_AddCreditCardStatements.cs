using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardStatements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "bonification_amount",
                table: "movement_payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bonification_type",
                table: "movement_payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "bonification_value",
                table: "movement_payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "net_amount",
                table: "movement_payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "statement_periods",
                columns: table => new
                {
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    previous_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    installments_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    charges_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bonifications_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    statement_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payments_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    pending_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statement_periods", x => new { x.family_id, x.statement_period_id });
                    table.ForeignKey(
                        name: "FK_statement_periods_credit_cards_family_id_credit_card_id",
                        columns: x => new { x.family_id, x.credit_card_id },
                        principalTable: "credit_cards",
                        principalColumns: new[] { "family_id", "credit_card_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_card_installments",
                columns: table => new
                {
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_card_installment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installment_number = table.Column<int>(type: "integer", nullable: false),
                    projected_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bonification_applied = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    effective_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    actual_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    estimated_date = table.Column<DateOnly>(type: "date", nullable: false),
                    statement_period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_card_installments", x => new { x.family_id, x.credit_card_installment_id });
                    table.ForeignKey(
                        name: "FK_credit_card_installments_movement_payments_family_id_moveme~",
                        columns: x => new { x.family_id, x.movement_payment_id },
                        principalTable: "movement_payments",
                        principalColumns: new[] { "family_id", "movement_payment_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_card_installments_statement_periods_family_id_statem~",
                        columns: x => new { x.family_id, x.statement_period_id },
                        principalTable: "statement_periods",
                        principalColumns: new[] { "family_id", "statement_period_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "statement_line_items",
                columns: table => new
                {
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_line_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statement_line_items", x => new { x.family_id, x.statement_line_item_id });
                    table.ForeignKey(
                        name: "FK_statement_line_items_statement_periods_family_id_statement_~",
                        columns: x => new { x.family_id, x.statement_period_id },
                        principalTable: "statement_periods",
                        principalColumns: new[] { "family_id", "statement_period_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "statement_payments",
                columns: table => new
                {
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    cash_box_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bank_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    primary_exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    secondary_exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    amount_in_primary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_in_secondary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_total_payment = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statement_payments", x => new { x.family_id, x.statement_payment_id });
                    table.ForeignKey(
                        name: "FK_statement_payments_statement_periods_family_id_statement_pe~",
                        columns: x => new { x.family_id, x.statement_period_id },
                        principalTable: "statement_periods",
                        principalColumns: new[] { "family_id", "statement_period_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "statement_payment_allocations",
                columns: table => new
                {
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_card_installment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    statement_line_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_card_currency = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_in_primary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_in_secondary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    primary_exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    secondary_exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CreditCardInstallmentFamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditCardInstallmentId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    StatementLineItemFamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    StatementLineItemId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statement_payment_allocations", x => new { x.family_id, x.allocation_id });
                    table.ForeignKey(
                        name: "FK_statement_payment_allocations_credit_card_installments_Cred~",
                        columns: x => new { x.CreditCardInstallmentFamilyId, x.CreditCardInstallmentId1 },
                        principalTable: "credit_card_installments",
                        principalColumns: new[] { "family_id", "credit_card_installment_id" });
                    table.ForeignKey(
                        name: "FK_statement_payment_allocations_statement_line_items_Statemen~",
                        columns: x => new { x.StatementLineItemFamilyId, x.StatementLineItemId1 },
                        principalTable: "statement_line_items",
                        principalColumns: new[] { "family_id", "statement_line_item_id" });
                    table.ForeignKey(
                        name: "FK_statement_payment_allocations_statement_payments_family_id_~",
                        columns: x => new { x.family_id, x.statement_payment_id },
                        principalTable: "statement_payments",
                        principalColumns: new[] { "family_id", "statement_payment_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cc_installments_payment",
                table: "credit_card_installments",
                columns: new[] { "family_id", "movement_payment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cc_installments_period",
                table: "credit_card_installments",
                columns: new[] { "family_id", "statement_period_id" },
                filter: "statement_period_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_statement_line_items_period",
                table: "statement_line_items",
                columns: new[] { "family_id", "statement_period_id" });

            migrationBuilder.CreateIndex(
                name: "ix_allocations_installment",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "credit_card_installment_id" },
                filter: "credit_card_installment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_allocations_line_item",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "statement_line_item_id" },
                filter: "statement_line_item_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_allocations_payment",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "statement_payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_statement_payment_allocations_CreditCardInstallmentFamilyId~",
                table: "statement_payment_allocations",
                columns: new[] { "CreditCardInstallmentFamilyId", "CreditCardInstallmentId1" });

            migrationBuilder.CreateIndex(
                name: "IX_statement_payment_allocations_StatementLineItemFamilyId_Sta~",
                table: "statement_payment_allocations",
                columns: new[] { "StatementLineItemFamilyId", "StatementLineItemId1" });

            migrationBuilder.CreateIndex(
                name: "ix_statement_payments_period",
                table: "statement_payments",
                columns: new[] { "family_id", "statement_period_id" });

            migrationBuilder.CreateIndex(
                name: "ix_statement_periods_one_open",
                table: "statement_periods",
                columns: new[] { "family_id", "credit_card_id" },
                unique: true,
                filter: "status = 'Open' AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "statement_payment_allocations");

            migrationBuilder.DropTable(
                name: "credit_card_installments");

            migrationBuilder.DropTable(
                name: "statement_line_items");

            migrationBuilder.DropTable(
                name: "statement_payments");

            migrationBuilder.DropTable(
                name: "statement_periods");

            migrationBuilder.DropColumn(
                name: "bonification_amount",
                table: "movement_payments");

            migrationBuilder.DropColumn(
                name: "bonification_type",
                table: "movement_payments");

            migrationBuilder.DropColumn(
                name: "bonification_value",
                table: "movement_payments");

            migrationBuilder.DropColumn(
                name: "net_amount",
                table: "movement_payments");
        }
    }
}
