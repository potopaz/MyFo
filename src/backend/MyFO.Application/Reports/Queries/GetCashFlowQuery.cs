using MediatR;
using MyFO.Application.Reports.DTOs;

namespace MyFO.Application.Reports.Queries;

public class GetCashFlowQuery : IRequest<CashFlowReportDto>
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Currency { get; set; } = string.Empty;
}
