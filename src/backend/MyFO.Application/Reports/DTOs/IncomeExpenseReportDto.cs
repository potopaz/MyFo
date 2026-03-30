namespace MyFO.Application.Reports.DTOs;

public class IncomeExpenseReportDto
{
    public string Granularity { get; set; } = "monthly"; // daily | weekly | monthly
    public decimal TotalExpense { get; set; }
    public decimal TotalIncome { get; set; }

    /// <summary>Top expenses by subcategory (for treemap / bar chart)</summary>
    public List<NameAmountDto> ExpenseBySubcategory { get; set; } = [];

    /// <summary>Expenses grouped by category (for outer treemap grouping)</summary>
    public List<CategoryExpenseDto> ExpenseByCategory { get; set; } = [];

    /// <summary>Ordinary vs Extraordinary expenses</summary>
    public OrdVsExtraDto OrdVsExtra { get; set; } = new();

    /// <summary>Expense evolution per top category over time</summary>
    public List<TimeSeriesMultiDto> CategoryEvolution { get; set; } = [];

    /// <summary>Income by subcategory (source)</summary>
    public List<NameAmountDto> IncomeBySource { get; set; } = [];

    /// <summary>Income evolution over time</summary>
    public List<TimePointDto> IncomeEvolution { get; set; } = [];
}

public class CategoryExpenseDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public List<NameAmountDto> Subcategories { get; set; } = [];
}
