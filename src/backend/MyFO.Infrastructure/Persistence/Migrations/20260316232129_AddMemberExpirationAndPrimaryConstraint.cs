using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberExpirationAndPrimaryConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_credit_card_members_family_id_credit_card_id",
                table: "credit_card_members");

            migrationBuilder.AddColumn<int>(
                name: "expiration_month",
                table: "credit_card_members",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "expiration_year",
                table: "credit_card_members",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_credit_card_members_one_primary",
                table: "credit_card_members",
                columns: new[] { "family_id", "credit_card_id" },
                unique: true,
                filter: "is_primary = true AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_credit_card_members_one_primary",
                table: "credit_card_members");

            migrationBuilder.DropColumn(
                name: "expiration_month",
                table: "credit_card_members");

            migrationBuilder.DropColumn(
                name: "expiration_year",
                table: "credit_card_members");

            migrationBuilder.CreateIndex(
                name: "IX_credit_card_members_family_id_credit_card_id",
                table: "credit_card_members",
                columns: new[] { "family_id", "credit_card_id" });
        }
    }
}
