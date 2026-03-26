namespace MyFO.Application.Reports.DTOs;

public class DisponibilidadesDto
{
    public string RequestedCurrency { get; set; } = string.Empty;
    /// <summary>Sum of all converted balances (accounts with no rate contribute 0).</summary>
    public decimal TotalConverted { get; set; }
    public List<CurrencyGroupDto> ByCurrency { get; set; } = [];
}

public class CurrencyGroupDto
{
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalNative { get; set; }
    /// <summary>null when no conversion rate was available for this currency.</summary>
    public decimal? TotalConverted { get; set; }
    public List<AccountBalanceDto> Accounts { get; set; } = [];
}

public class AccountBalanceDto
{
    /// <summary>"CashBox" or "BankAccount"</summary>
    public string AccountType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>null when no conversion rate was available.</summary>
    public decimal? BalanceConverted { get; set; }
}
