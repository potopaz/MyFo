using MyFO.Application.Common.Mediator;
using MyFO.Application.Accounting.CostCenters.DTOs;

namespace MyFO.Application.Accounting.CostCenters.Commands;

public record CreateCostCenterCommand(
    string Name
) : IRequest<CostCenterDto>;
