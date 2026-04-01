using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- Operations from FixStatementPaymentAllocations that may not have applied ----

            // Add movement_payment_id if missing
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations ADD COLUMN IF NOT EXISTS movement_payment_id uuid");

            // Drop shadow columns if they still exist
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP COLUMN IF EXISTS \"CreditCardInstallmentFamilyId\"");
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP COLUMN IF EXISTS \"CreditCardInstallmentId1\"");
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP COLUMN IF EXISTS \"StatementLineItemFamilyId\"");
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP COLUMN IF EXISTS \"StatementLineItemId1\"");

            // Fix exchange rate precision to (18,8)
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations ALTER COLUMN primary_exchange_rate TYPE numeric(18,8)");
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations ALTER COLUMN secondary_exchange_rate TYPE numeric(18,8)");

            // Rename payment index if old name exists
            migrationBuilder.Sql(@"DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname='txn' AND indexname='ix_allocations_payment') THEN
                    ALTER INDEX txn.ix_allocations_payment RENAME TO ix_statement_payment_allocations_payment;
                END IF;
            END $$");

            // ---- FK and index cleanup (drop all variants before recreating) ----

            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP CONSTRAINT IF EXISTS fk_spa_installment");
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP CONSTRAINT IF EXISTS fk_spa_line_item");
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP CONSTRAINT IF EXISTS fk_spa_movement_payment");
            migrationBuilder.Sql("DROP INDEX IF EXISTS txn.ix_spa_movement_payment");

            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP CONSTRAINT IF EXISTS \"FK_statement_payment_allocations_credit_card_installments_fami~\"");
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP CONSTRAINT IF EXISTS \"FK_statement_payment_allocations_movement_payments_family_id_m~\"");
            migrationBuilder.Sql("ALTER TABLE txn.statement_payment_allocations DROP CONSTRAINT IF EXISTS \"FK_statement_payment_allocations_statement_line_items_family_i~\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"txn\".\"IX_statement_payment_allocations_family_id_credit_card_install~\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"txn\".\"IX_statement_payment_allocations_family_id_movement_payment_id\"");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"txn\".\"IX_statement_payment_allocations_family_id_statement_line_item~\"");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_statement_payment_allocations_family_id_credit_card_install~\" ON txn.statement_payment_allocations (family_id, credit_card_installment_id)");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_statement_payment_allocations_family_id_movement_payment_id\" ON txn.statement_payment_allocations (family_id, movement_payment_id)");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_statement_payment_allocations_family_id_statement_line_item~\" ON txn.statement_payment_allocations (family_id, statement_line_item_id)");

            migrationBuilder.Sql(@"DO $$ BEGIN
                ALTER TABLE txn.statement_payment_allocations
                    ADD CONSTRAINT ""FK_statement_payment_allocations_credit_card_installments_fami~""
                    FOREIGN KEY (family_id, credit_card_installment_id)
                    REFERENCES txn.credit_card_installments (family_id, credit_card_installment_id);
            EXCEPTION WHEN duplicate_object THEN NULL;
            END $$");

            migrationBuilder.Sql(@"DO $$ BEGIN
                ALTER TABLE txn.statement_payment_allocations
                    ADD CONSTRAINT ""FK_statement_payment_allocations_movement_payments_family_id_m~""
                    FOREIGN KEY (family_id, movement_payment_id)
                    REFERENCES txn.movement_payments (family_id, movement_payment_id);
            EXCEPTION WHEN duplicate_object THEN NULL;
            END $$");

            migrationBuilder.Sql(@"DO $$ BEGIN
                ALTER TABLE txn.statement_payment_allocations
                    ADD CONSTRAINT ""FK_statement_payment_allocations_statement_line_items_family_i~""
                    FOREIGN KEY (family_id, statement_line_item_id)
                    REFERENCES txn.statement_line_items (family_id, statement_line_item_id);
            EXCEPTION WHEN duplicate_object THEN NULL;
            END $$");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_statement_payment_allocations_credit_card_installments_fami~",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_statement_payment_allocations_movement_payments_family_id_m~",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_statement_payment_allocations_statement_line_items_family_i~",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_statement_payment_allocations_family_id_credit_card_install~",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_statement_payment_allocations_family_id_movement_payment_id",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_statement_payment_allocations_family_id_statement_line_item~",
                schema: "txn",
                table: "statement_payment_allocations");

            migrationBuilder.Sql("DROP INDEX IF EXISTS txn.ix_spa_movement_payment");
            migrationBuilder.CreateIndex(
                name: "ix_spa_movement_payment",
                schema: "txn",
                table: "statement_payment_allocations",
                columns: new[] { "family_id", "movement_payment_id" },
                filter: "movement_payment_id IS NOT NULL");

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
        }
    }
}
