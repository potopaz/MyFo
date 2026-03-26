namespace MyFO.Application.Reports.DTOs;

public class DashboardSummaryDto
{
    public decimal Patrimony { get; set; }
    public decimal PatrimonyChange { get; set; }
    public decimal MonthIncome { get; set; }
    public decimal MonthExpense { get; set; }
    public decimal MonthResult { get; set; }
    public decimal? MonthIncomeChangePct { get; set; }
    public decimal? MonthExpenseChangePct { get; set; }
    public List<MonthlyFlowDto> MonthlyFlow { get; set; } = [];
    public List<MonthlyPatrimonyDto> PatrimonyEvolution { get; set; } = [];
}

public class MonthlyFlowDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Result { get; set; }
}

public class MonthlyPatrimonyDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Balance { get; set; }
}
