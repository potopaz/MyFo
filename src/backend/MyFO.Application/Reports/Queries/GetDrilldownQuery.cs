using MediatR;
using MyFO.Application.Reports.DTOs;

namespace MyFO.Application.Reports.Queries;

public class GetDrilldownQuery : IRequest<DrilldownResultDto>
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Currency { get; set; } = string.Empty;

    // One of these must be set to filter
    public string? Dimension { get; set; }   // "category" | "subcategory" | "costcenter" | "ordinary"
    public string? DimensionValue { get; set; }  // name or ID

    public string? MovementType { get; set; } // "Income" | "Expense" | null (both)

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
