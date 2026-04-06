using MyFO.Domain.Common;

namespace MyFO.Domain.Transactions;

/// <summary>
/// A bank account. Name should include the bank, e.g. "Cuenta corriente Banco Nación".
/// Each account holds a single currency.
/// </summary>
public class BankAccount : TenantEntity
{
    public Guid BankAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal Balance { get; set; }
    public string? AccountNumber { get; set; }
    public string? Cbu { get; set; }
    public string? Alias { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsInitialBalanceReconciled { get; set; }
}
