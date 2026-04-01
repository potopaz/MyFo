using MyFO.Application.Common.Mediator;
using MyFO.Application.Accounting.CostCenters.DTOs;

namespace MyFO.Application.Accounting.CostCenters.Commands;

public class UpdateCostCenterCommand : IRequest<CostCenterDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid CostCenterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
