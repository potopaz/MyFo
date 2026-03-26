using MediatR;
using MyFO.Application.Accounting.CostCenters.DTOs;

namespace MyFO.Application.Accounting.CostCenters.Queries;

public record GetCostCentersQuery : IRequest<List<CostCenterDto>>;
