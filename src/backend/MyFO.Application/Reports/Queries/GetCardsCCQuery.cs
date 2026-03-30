using MediatR;
using MyFO.Application.Reports.DTOs;

namespace MyFO.Application.Reports.Queries;

public class GetCardsCCQuery : IRequest<CardsCCReportDto>
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Currency { get; set; } = string.Empty;
}
