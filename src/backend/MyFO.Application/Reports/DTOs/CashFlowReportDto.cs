namespace MyFO.Application.Reports.DTOs;

public class CashFlowReportDto
{
    public string Granularity { get; set; } = "monthly";

    /// <summary>Net cash flow over time (income - expense)</summary>
    public List<CashFlowPointDto> CashFlow { get; set; } = [];

    /// <summary>Upcoming credit card installments for next 12 months</summary>
    public List<FutureInstallmentDto> FutureInstallments { get; set; } = [];

    /// <summary>Expense breakdown by payment method type</summary>
    public List<NameAmountDto> PaymentMethods { get; set; } = [];

    /// <summary>Payment method evolution (stacked % per period)</summary>
    public List<TimeSeriesMultiDto> PaymentMethodEvolution { get; set; } = [];
}

public class CashFlowPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net { get; set; }
}

public class FutureInstallmentDto
{
    public string Label { get; set; } = string.Empty;   // "Abr 2026"
    public string Month { get; set; } = string.Empty;   // "2026-04" (for frontend filtering)
    public decimal Amount { get; set; }
    public string CardName { get; set; } = string.Empty;
}
