using System.Text.Json.Serialization;
using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.Movements.DTOs;

namespace MyFO.Application.Transactions.Movements.Commands;

public class UpdateMovementCommand : IRequest<MovementDto>
{
    [JsonIgnore]
    public Guid MovementId { get; set; }
    public DateOnly Date { get; set; }
    public string? MovementType { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal PrimaryExchangeRate { get; set; } = 1m;
    public decimal SecondaryExchangeRate { get; set; } = 1m;
    public string? Description { get; set; }
    public Guid SubcategoryId { get; set; }
    public string? AccountingType { get; set; }
    public bool? IsOrdinary { get; set; }
    public Guid? CostCenterId { get; set; }
    public List<CreateMovementPaymentItem> Payments { get; set; } = [];
    public int? ClientRowVersion { get; set; }
}
