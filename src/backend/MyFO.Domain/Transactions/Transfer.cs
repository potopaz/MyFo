using MyFO.Domain.Common;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Domain.Transactions;

public class Transfer : TenantEntity
{
    public Guid TransferId { get; set; }
    public DateOnly Date { get; set; }
    public Guid? FromCashBoxId { get; set; }
    public Guid? FromBankAccountId { get; set; }
    public Guid? ToCashBoxId { get; set; }
    public Guid? ToBankAccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal FromPrimaryExchangeRate { get; set; } = 1m;
    public decimal FromSecondaryExchangeRate { get; set; } = 1m;
    public decimal ToPrimaryExchangeRate { get; set; } = 1m;
    public decimal ToSecondaryExchangeRate { get; set; } = 1m;
    public decimal AmountTo { get; set; }
    public decimal AmountToInPrimary { get; set; }
    public decimal AmountToInSecondary { get; set; }
    public decimal AmountInPrimary { get; set; }
    public decimal AmountInSecondary { get; set; }
    public string? Description { get; set; }
    public string Source { get; set; } = "Web";
    public int RowVersion { get; set; } = 1;
    public TransferStatus Status { get; set; } = TransferStatus.Confirmed;
    public bool IsAutoConfirmed { get; set; } = true;
    public string? RejectionComment { get; set; }
    public bool IsReconciled { get; set; }

    public CashBox? FromCashBox { get; set; }
    public BankAccount? FromBankAccount { get; set; }
    public CashBox? ToCashBox { get; set; }
    public BankAccount? ToBankAccount { get; set; }
}
