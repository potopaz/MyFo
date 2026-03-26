namespace MyFO.Application.Transactions.BankAccounts.DTOs;

public class BankAccountDto
{
    public Guid BankAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal Balance { get; set; }
    public string? AccountNumber { get; set; }
    public string? Cbu { get; set; }
    public string? Alias { get; set; }
    public bool IsActive { get; set; }
}
