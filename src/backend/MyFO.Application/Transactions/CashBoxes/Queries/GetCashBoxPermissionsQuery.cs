using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.CashBoxes.DTOs;

namespace MyFO.Application.Transactions.CashBoxes.Queries;

public record GetCashBoxPermissionsQuery(Guid CashBoxId) : IRequest<List<CashBoxMemberPermissionDto>>;
