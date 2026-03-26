using MediatR;
using MyFO.Application.Transactions.CashBoxes.DTOs;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public class UpsertCashBoxPermissionCommand : IRequest<CashBoxMemberPermissionDto>
{
    public Guid CashBoxId { get; set; }
    public Guid MemberId { get; set; }
}
