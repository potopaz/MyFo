using MyFO.Application.Common.Mediator;
using MyFO.Application.Reports.DTOs;

namespace MyFO.Application.Reports.Queries;

public class GetDisponibilidadesQuery : IRequest<DisponibilidadesDto>
{
    public string Currency { get; set; } = string.Empty;
}
