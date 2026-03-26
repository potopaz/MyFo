using MediatR;
using MyFO.Application.Transactions.Movements.DTOs;

namespace MyFO.Application.Transactions.Movements.Queries;

public class GetMovementsQuery : IRequest<List<MovementListItemDto>>
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? MovementType { get; set; }
    public Guid? SubcategoryId { get; set; }
    public string? Description { get; set; }
}
