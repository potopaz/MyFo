using MyFO.Domain.Common;

namespace MyFO.Domain.Transactions;

/// <summary>
/// A physical cash box or wallet.
/// Each cash box holds money in a single currency.
/// Examples: "Billetera ARS", "Cash USD"
/// </summary>
public class CashBox : TenantEntity
{
    public Guid CashBoxId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; } = true;
}
