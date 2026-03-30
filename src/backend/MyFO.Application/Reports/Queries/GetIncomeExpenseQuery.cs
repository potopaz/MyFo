using MediatR;
using MyFO.Application.Reports.DTOs;

namespace MyFO.Application.Reports.Queries;

public class GetIncomeExpenseQuery : IRequest<IncomeExpenseReportDto>
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? SubcategoryId { get; set; }
    public Guid? CostCenterId { get; set; }
    public bool? IsOrdinary { get; set; }
}
