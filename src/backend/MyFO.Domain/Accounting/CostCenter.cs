using MyFO.Domain.Common;

namespace MyFO.Domain.Accounting;

/// <summary>
/// A cost center for tracking expenses by area/project.
/// Examples: "Casa principal", "Departamento alquiler", "Negocio"
/// </summary>
public class CostCenter : TenantEntity
{
    public Guid CostCenterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
