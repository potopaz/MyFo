namespace MyFO.Application.Reports.DTOs;

public class PatrimonyReportDto
{
    // ── KPIs ──────────────────────────────────────────────────────────────────
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetPatrimony { get; set; }
    public decimal PeriodIncome { get; set; }
    public decimal PeriodExpense { get; set; }
    public decimal PeriodSavings { get; set; }
    public decimal? SavingsRatio { get; set; }

    // ── Charts ────────────────────────────────────────────────────────────────

    /// <summary>Net patrimony evolution by month</summary>
    public List<TimePointDto> PatrimonyEvolution { get; set; } = [];

    /// <summary>Balance breakdown (cash boxes + bank accounts, by currency)</summary>
    public List<NameAmountDto> BalanceByCurrency { get; set; } = [];

    /// <summary>Balance breakdown by account type (Efectivo / Banco)</summary>
    public List<NameAmountDto> BalanceByAccountType { get; set; } = [];

    /// <summary>Top accounts by balance</summary>
    public List<AccountBalanceItemDto> TopAccounts { get; set; } = [];
}

public class AccountBalanceItemDto
{
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // CashBox | BankAccount
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal BalanceInReportCurrency { get; set; }
}
