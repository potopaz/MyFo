using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecreateTransfersWithToExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS transfers CASCADE;");

            migrationBuilder.Sql(@"
CREATE TABLE transfers (
    family_id                    uuid                        NOT NULL,
    transfer_id                  uuid                        NOT NULL DEFAULT gen_random_uuid(),
    date                         date                        NOT NULL,
    from_cash_box_id             uuid,
    from_bank_account_id         uuid,
    to_cash_box_id               uuid,
    to_bank_account_id           uuid,
    amount                       numeric(18,2)               NOT NULL,
    exchange_rate                numeric(18,8)               NOT NULL DEFAULT 1,
    from_primary_exchange_rate   numeric(18,8)               NOT NULL DEFAULT 1,
    from_secondary_exchange_rate numeric(18,8)               NOT NULL DEFAULT 1,
    to_primary_exchange_rate     numeric(18,8)               NOT NULL DEFAULT 1,
    to_secondary_exchange_rate   numeric(18,8)               NOT NULL DEFAULT 1,
    amount_to                    numeric(18,2)               NOT NULL,
    amount_in_primary            numeric(18,2)               NOT NULL,
    amount_in_secondary          numeric(18,2)               NOT NULL DEFAULT 0,
    amount_to_in_primary         numeric(18,2)               NOT NULL,
    amount_to_in_secondary       numeric(18,2)               NOT NULL DEFAULT 0,
    description                  character varying(500),
    source                       text                        NOT NULL DEFAULT 'Web',
    status                       integer                     NOT NULL DEFAULT 0,
    is_auto_confirmed            boolean                     NOT NULL DEFAULT true,
    rejection_comment            text,
    created_at                   timestamp with time zone    NOT NULL,
    created_by                   uuid                        NOT NULL,
    modified_at                  timestamp with time zone,
    modified_by                  uuid,
    deleted_at                   timestamp with time zone,
    deleted_by                   uuid,
    CONSTRAINT ""PK_transfers"" PRIMARY KEY (family_id, transfer_id),
    CONSTRAINT ""FK_transfers_cash_boxes_family_id_from_cash_box_id""
        FOREIGN KEY (family_id, from_cash_box_id) REFERENCES cash_boxes (family_id, cash_box_id),
    CONSTRAINT ""FK_transfers_cash_boxes_family_id_to_cash_box_id""
        FOREIGN KEY (family_id, to_cash_box_id) REFERENCES cash_boxes (family_id, cash_box_id),
    CONSTRAINT ""FK_transfers_bank_accounts_family_id_from_bank_account_id""
        FOREIGN KEY (family_id, from_bank_account_id) REFERENCES bank_accounts (family_id, bank_account_id),
    CONSTRAINT ""FK_transfers_bank_accounts_family_id_to_bank_account_id""
        FOREIGN KEY (family_id, to_bank_account_id) REFERENCES bank_accounts (family_id, bank_account_id)
);

CREATE INDEX ix_transfers_family_date
    ON transfers (family_id, date);

CREATE INDEX ""IX_transfers_family_id_from_cash_box_id""
    ON transfers (family_id, from_cash_box_id);

CREATE INDEX ""IX_transfers_family_id_to_cash_box_id""
    ON transfers (family_id, to_cash_box_id);

CREATE INDEX ""IX_transfers_family_id_from_bank_account_id""
    ON transfers (family_id, from_bank_account_id);

CREATE INDEX ""IX_transfers_family_id_to_bank_account_id""
    ON transfers (family_id, to_bank_account_id);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS transfers CASCADE;");
        }
    }
}
