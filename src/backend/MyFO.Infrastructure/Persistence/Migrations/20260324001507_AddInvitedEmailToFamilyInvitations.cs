using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitedEmailToFamilyInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "invited_email",
                schema: "cfg",
                table: "family_invitations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_family_invitations_family_email",
                schema: "cfg",
                table: "family_invitations",
                columns: new[] { "family_id", "invited_email" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_family_invitations_family_email",
                schema: "cfg",
                table: "family_invitations");

            migrationBuilder.DropColumn(
                name: "invited_email",
                schema: "cfg",
                table: "family_invitations");
        }
    }
}
