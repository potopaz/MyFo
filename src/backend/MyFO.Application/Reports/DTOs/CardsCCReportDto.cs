namespace MyFO.Application.Reports.DTOs;

public class CardsCCReportDto
{
    /// <summary>Total outstanding installments (future debt)</summary>
    public decimal TotalDebt { get; set; }

    /// <summary>Total paid in period</summary>
    public decimal TotalPaid { get; set; }

    /// <summary>Pending installments by card</summary>
    public List<CardInstallmentsSummaryDto> InstallmentsByCard { get; set; } = [];

    /// <summary>Expense by cost center (donut)</summary>
    public List<NameAmountDto> ByCostCenter { get; set; } = [];

    /// <summary>Cost center evolution over time</summary>
    public List<TimeSeriesMultiDto> CostCenterEvolution { get; set; } = [];

    /// <summary>Charges vs bonifications from statement line items</summary>
    public ChargesVsBonificationsDto ChargesVsBonifications { get; set; } = new();

    public string Granularity { get; set; } = "monthly";
}

public class CardInstallmentsSummaryDto
{
    public string CardName { get; set; } = string.Empty;
    public decimal TotalDebt { get; set; }
    public int PendingInstallments { get; set; }
}

public class ChargesVsBonificationsDto
{
    public decimal TotalCharges { get; set; }
    public decimal TotalBonifications { get; set; }
    public decimal Net { get; set; }
}
