using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBimonetary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set USD as secondary currency for families that don't have one yet
            migrationBuilder.Sql("UPDATE families SET secondary_currency_code = 'USD' WHERE secondary_currency_code IS NULL");

            migrationBuilder.DropColumn(
                name: "is_bimonetary",
                table: "families");

            migrationBuilder.AlterColumn<string>(
                name: "secondary_currency_code",
                table: "families",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "secondary_currency_code",
                table: "families",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AddColumn<bool>(
                name: "is_bimonetary",
                table: "families",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
