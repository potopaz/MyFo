namespace MyFO.Application.Reports.DTOs;

public class CardsCCReportDto
{
    /// <summary>Total outstanding installments (future debt)</summary>
    public decimal TotalDebt { get; set; }

    /// <summary>Total paid in period</summary>
    public decimal TotalPaid { get; set; }

    /// <summary>Pending installments by card (future debt summary)</summary>
    public List<CardInstallmentsSummaryDto> InstallmentsByCard { get; set; } = [];

    /// <summary>Future installments per month per card (for stacked bar chart)</summary>
    public List<FutureInstallmentDto> FutureInstallments { get; set; } = [];

    /// <summary>Expense by cost center (donut)</summary>
    public List<NameAmountDto> ByCostCenter { get; set; } = [];

    /// <summary>Cost center evolution over time</summary>
    public List<TimeSeriesMultiDto> CostCenterEvolution { get; set; } = [];

    /// <summary>Charges vs bonifications from statement line items</summary>
    public ChargesVsBonificationsDto ChargesVsBonifications { get; set; } = new();

    public string Granularity { get; set; } = "monthly";

    /// <summary>Monthly new CC debt vs payments (for evolution bar+line chart)</summary>
    public List<MonthlyDebtEvolutionDto> MonthlyDebtEvolution { get; set; } = [];
}

public class CardInstallmentsSummaryDto
{
    public Guid CardId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public decimal TotalDebt { get; set; }
    public decimal TotalPaid { get; set; }
    public int PendingInstallments { get; set; }
}

public class MonthlyDebtEvolutionDto
{
    public string Label { get; set; } = string.Empty;
    public decimal NewDebt { get; set; }
    public decimal Paid { get; set; }
    public decimal Net { get; set; }
}

public class ChargesVsBonificationsDto
{
    public decimal TotalCharges { get; set; }
    public decimal TotalBonifications { get; set; }
    public decimal Net { get; set; }
}
