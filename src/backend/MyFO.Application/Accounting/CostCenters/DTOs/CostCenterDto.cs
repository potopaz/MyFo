namespace MyFO.Application.Accounting.CostCenters.DTOs;

public class CostCenterDto
{
    public Guid CostCenterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
