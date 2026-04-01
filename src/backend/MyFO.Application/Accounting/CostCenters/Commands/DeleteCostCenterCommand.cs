using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Accounting.CostCenters.Commands;

public record DeleteCostCenterCommand(Guid CostCenterId) : IRequest;
