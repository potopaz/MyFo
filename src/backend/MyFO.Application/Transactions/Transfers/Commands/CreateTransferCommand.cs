using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.Transfers.DTOs;

namespace MyFO.Application.Transactions.Transfers.Commands;

public class CreateTransferCommand : IRequest<TransferDto>
{
    public DateOnly Date { get; set; }
    public Guid? FromCashBoxId { get; set; }
    public Guid? FromBankAccountId { get; set; }
    public Guid? ToCashBoxId { get; set; }
    public Guid? ToBankAccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal FromPrimaryExchangeRate { get; set; } = 1m;
    public decimal FromSecondaryExchangeRate { get; set; } = 1m;
    public decimal ToPrimaryExchangeRate { get; set; } = 1m;
    public decimal ToSecondaryExchangeRate { get; set; } = 1m;
    public decimal AmountTo { get; set; }
    public string? Description { get; set; }
    public string Source { get; set; } = "Web";
}
