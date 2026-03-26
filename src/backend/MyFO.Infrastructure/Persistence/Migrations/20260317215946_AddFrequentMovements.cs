using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrequentMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "frequent_movements",
                columns: table => new
                {
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frequent_movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    subcategory_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accounting_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_ordinary = table.Column<bool>(type: "boolean", nullable: true),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_method_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cash_box_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bank_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_card_id = table.Column<Guid>(type: "uuid", nullable: true),
                    credit_card_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    frequency_months = table.Column<int>(type: "integer", nullable: true),
                    last_applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_frequent_movements", x => new { x.family_id, x.frequent_movement_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_frequent_movements_family_active",
                table: "frequent_movements",
                columns: new[] { "family_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "frequent_movements");
        }
    }
}
