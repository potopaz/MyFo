using MyFO.Application.Common.Mediator;
using MyFO.Application.Reports.DTOs;

namespace MyFO.Application.Reports.Queries;

public class GetPeriodAnalysisQuery : IRequest<PeriodAnalysisDto>
{
    public string Period { get; set; } = "mes-actual";
    public string Currency { get; set; } = string.Empty;
}
