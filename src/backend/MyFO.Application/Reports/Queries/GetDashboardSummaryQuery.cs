using MediatR;
using MyFO.Application.Reports.DTOs;

namespace MyFO.Application.Reports.Queries;

public class GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>
{
    public string Currency { get; set; } = string.Empty;
}
