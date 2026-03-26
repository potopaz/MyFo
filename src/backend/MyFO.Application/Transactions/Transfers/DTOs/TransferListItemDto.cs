namespace MyFO.Application.Transactions.Transfers.DTOs;

public class TransferListItemDto
{
    public string Status { get; set; } = "Confirmed";
    public bool IsAutoConfirmed { get; set; } = true;
    public string? RejectionComment { get; set; }
    public string? CreatorUserId { get; set; }
    public Guid TransferId { get; set; }
    public DateOnly Date { get; set; }
    public Guid? FromCashBoxId { get; set; }
    public string? FromCashBoxName { get; set; }
    public Guid? FromBankAccountId { get; set; }
    public string? FromBankAccountName { get; set; }
    public Guid? ToCashBoxId { get; set; }
    public string? ToCashBoxName { get; set; }
    public Guid? ToBankAccountId { get; set; }
    public string? ToBankAccountName { get; set; }
    public string FromCurrencyCode { get; set; } = string.Empty;
    public string ToCurrencyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal FromPrimaryExchangeRate { get; set; }
    public decimal FromSecondaryExchangeRate { get; set; }
    public decimal ToPrimaryExchangeRate { get; set; }
    public decimal ToSecondaryExchangeRate { get; set; }
    public decimal AmountTo { get; set; }
    public decimal AmountToInPrimary { get; set; }
    public decimal AmountToInSecondary { get; set; }
    public decimal AmountInPrimary { get; set; }
    public decimal AmountInSecondary { get; set; }
    public string? Description { get; set; }
    public int RowVersion { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedByName { get; set; }
}
