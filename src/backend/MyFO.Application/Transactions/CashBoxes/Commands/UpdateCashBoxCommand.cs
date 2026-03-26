using MediatR;
using MyFO.Application.Transactions.CashBoxes.DTOs;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public class UpdateCashBoxCommand : IRequest<CashBoxDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid CashBoxId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public bool IsActive { get; set; } = true;
}
