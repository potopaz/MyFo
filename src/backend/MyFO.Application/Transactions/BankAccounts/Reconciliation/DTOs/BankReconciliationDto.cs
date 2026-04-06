namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.DTOs;

public class BankReconciliationDto
{
    public Guid BankAccountId { get; set; }
    public string BankAccountName { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
    public decimal PreviousReconciledBalance { get; set; }
    public List<BankReconciliationItemDto> Items { get; set; } = [];
}

public class BankReconciliationItemDto
{
    /// <summary>InitialBalance | MovementPayment | Transfer | CreditCardPayment</summary>
    public string Type { get; set; } = "";
    public Guid Id { get; set; }
    public DateOnly? Date { get; set; }
    public string Description { get; set; } = "";
    public decimal? Credit { get; set; }
    public decimal? Debit { get; set; }
    public bool IsReconciled { get; set; }
    public bool IsOutsideDateRange { get; set; }
}
