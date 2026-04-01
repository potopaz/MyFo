using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixStatementPaymentAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop shadow column indexes
            migrationBuilder.DropIndex(
                name: "IX_statement_payment_allocations_CreditCardInstallmentFamilyId~",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_statement_payment_allocations_StatementLineItemFamilyId_Sta~",
                schema: "txn",
                table: "statement_payment_allocations");

            // Drop old installment/line_item indexes
            migrationBuilder.DropIndex(
                name: "ix_allocations_installment",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropIndex(
                name: "ix_allocations_line_item",
                schema: "txn",
                table: "statement_payment_allocations");

            // Drop FK constraints using shadow columns
            migrationBuilder.DropForeignKey(
                name: "FK_statement_payment_allocations_credit_card_installments_Cred~",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_statement_payment_allocations_statement_line_items_Statemen~",
                schema: "txn",
                table: "statement_payment_allocations");

            // Drop shadow columns
            migrationBuilder.DropColumn(
                name: "CreditCardInstallmentFamilyId",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropColumn(
                name: "CreditCardInstallmentId1",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropColumn(
                name: "StatementLineItemFamilyId",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropColumn(
                name: "StatementLineItemId1",
                schema: "txn",
                table: "statement_payment_allocations");

            // Add movement_payment_id for installment bonification rows
            migrationBuilder.AddColumn<Guid>(
                name: "movement_payment_id",
                schema: "txn",
                table: "statement_payment_allocations",
                type: "uuid",
                nullable: true);

            // Fix exchange rate precision: (18,6) → (18,8)
            migrationBuilder.AlterColumn<decimal>(
                name: "primary_exchange_rate",
                schema: "txn",
                table: "statement_payment_allocations",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "secondary_exchange_rate",
                schema: "txn",
                table: "statement_payment_allocations",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            // Rename payment index to consistent naming
            migrationBuilder.RenameIndex(
                name: "ix_allocations_payment",
                schema: "txn",
                table: "statement_payment_allocations",
                newName: "ix_statement_payment_allocations_payment");

            // Add proper composite FK constraints
            migrationBuilder.AddForeignKey(
                name: "fk_spa_installment",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "credit_card_installment_id" },
                principalSchema: "txn",
                principalTable: "credit_card_installments",
                principalColumns: new[] { "family_id", "credit_card_installment_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_spa_line_item",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "statement_line_item_id" },
                principalSchema: "txn",
                principalTable: "statement_line_items",
                principalColumns: new[] { "family_id", "statement_line_item_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_spa_movement_payment",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "movement_payment_id" },
                principalSchema: "txn",
                principalTable: "movement_payments",
                principalColumns: new[] { "family_id", "movement_payment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_spa_movement_payment",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "movement_payment_id" },
                filter: "movement_payment_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_spa_installment",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_spa_line_item",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_spa_movement_payment",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropIndex(
                name: "ix_spa_movement_payment",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropColumn(
                name: "movement_payment_id",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.RenameIndex(
                name: "ix_statement_payment_allocations_payment",
                schema: "txn",
                table: "statement_payment_allocations",
                newName: "ix_allocations_payment");

            migrationBuilder.AlterColumn<decimal>(
                name: "secondary_exchange_rate",
                schema: "txn",
                table: "statement_payment_allocations",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "primary_exchange_rate",
                schema: "txn",
                table: "statement_payment_allocations",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AddColumn<Guid>(
                name: "StatementLineItemId1",
                schema: "txn",
                table: "statement_payment_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StatementLineItemFamilyId",
                schema: "txn",
                table: "statement_payment_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreditCardInstallmentId1",
                schema: "txn",
                table: "statement_payment_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreditCardInstallmentFamilyId",
                schema: "txn",
                table: "statement_payment_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_allocations_installment",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "credit_card_installment_id" },
                filter: "credit_card_installment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_allocations_line_item",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "statement_line_item_id" },
                filter: "statement_line_item_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_statement_payment_allocations_CreditCardInstallmentFamilyId~",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "CreditCardInstallmentFamilyId", "CreditCardInstallmentId1" });

            migrationBuilder.CreateIndex(
                name: "IX_statement_payment_allocations_StatementLineItemFamilyId_Sta~",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "StatementLineItemFamilyId", "StatementLineItemId1" });

            migrationBuilder.AddForeignKey(
                name: "FK_statement_payment_allocations_credit_card_installments_Cred~",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "CreditCardInstallmentFamilyId", "CreditCardInstallmentId1" },
                principalSchema: "txn",
                principalTable: "credit_card_installments",
                principalColumns: new[] { "family_id", "credit_card_installment_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_statement_payment_allocations_statement_line_items_Statemen~",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "StatementLineItemFamilyId", "StatementLineItemId1" },
                principalSchema: "txn",
                principalTable: "statement_line_items",
                principalColumns: new[] { "family_id", "statement_line_item_id" });
        }
    }
}
