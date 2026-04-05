using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.Movements.Commands;

public class PatchMovementClassificationCommand : IRequest
{
    public Guid MovementId { get; set; }
    public Guid SubcategoryId { get; set; }
    public string? AccountingType { get; set; }
    public bool? IsOrdinary { get; set; }
    public Guid? CostCenterId { get; set; }
    public int RowVersion { get; set; }
}
