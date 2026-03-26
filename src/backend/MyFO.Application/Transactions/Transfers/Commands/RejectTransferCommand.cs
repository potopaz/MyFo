using MediatR;

namespace MyFO.Application.Transactions.Transfers.Commands;

public class RejectTransferCommand : IRequest
{
    public Guid TransferId { get; set; }
    public string? Comment { get; set; }
}
