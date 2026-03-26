using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardPaymentConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE movement_payments
                ADD CONSTRAINT chk_credit_card_member_required
                CHECK (payment_method_type <> 'CreditCard' OR credit_card_member_id IS NOT NULL);
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE movement_payments
                ADD CONSTRAINT chk_installments_positive
                CHECK (installments IS NULL OR installments >= 1);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE movement_payments DROP CONSTRAINT chk_installments_positive;");
            migrationBuilder.Sql("ALTER TABLE movement_payments DROP CONSTRAINT chk_credit_card_member_required;");
        }
    }
}
