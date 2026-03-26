using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    from_cash_box_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_bank_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_cash_box_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_bank_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1m),
                    secondary_exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1m),
                    amount_to = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_in_primary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_in_secondary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => new { x.family_id, x.transfer_id });
                    table.ForeignKey(
                        name: "FK_transfers_bank_accounts_family_id_from_bank_account_id",
                        columns: x => new { x.family_id, x.from_bank_account_id },
                        principalTable: "bank_accounts",
                        principalColumns: new[] { "family_id", "bank_account_id" });
                    table.ForeignKey(
                        name: "FK_transfers_bank_accounts_family_id_to_bank_account_id",
                        columns: x => new { x.family_id, x.to_bank_account_id },
                        principalTable: "bank_accounts",
                        principalColumns: new[] { "family_id", "bank_account_id" });
                    table.ForeignKey(
                        name: "FK_transfers_cash_boxes_family_id_from_cash_box_id",
                        columns: x => new { x.family_id, x.from_cash_box_id },
                        principalTable: "cash_boxes",
                        principalColumns: new[] { "family_id", "cash_box_id" });
                    table.ForeignKey(
                        name: "FK_transfers_cash_boxes_family_id_to_cash_box_id",
                        columns: x => new { x.family_id, x.to_cash_box_id },
                        principalTable: "cash_boxes",
                        principalColumns: new[] { "family_id", "cash_box_id" });
                });

            migrationBuilder.CreateIndex(
                name: "ix_transfers_family_date",
                table: "transfers",
                columns: new[] { "family_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_transfers_family_id_from_bank_account_id",
                table: "transfers",
                columns: new[] { "family_id", "from_bank_account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transfers_family_id_from_cash_box_id",
                table: "transfers",
                columns: new[] { "family_id", "from_cash_box_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transfers_family_id_to_bank_account_id",
                table: "transfers",
                columns: new[] { "family_id", "to_bank_account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_transfers_family_id_to_cash_box_id",
                table: "transfers",
                columns: new[] { "family_id", "to_cash_box_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transfers");
        }
    }
}
