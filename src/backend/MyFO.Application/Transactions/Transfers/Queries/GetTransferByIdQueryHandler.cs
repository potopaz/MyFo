using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.Transfers.DTOs;
using MyFO.Domain.Exceptions;

namespace MyFO.Application.Transactions.Transfers.Queries;

public class GetTransferByIdQueryHandler : IRequestHandler<GetTransferByIdQuery, TransferDto>
{
    private readonly IApplicationDbContext _db;

    public GetTransferByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<TransferDto> Handle(GetTransferByIdQuery request, CancellationToken cancellationToken)
    {
        var transfer = await _db.Transfers
            .Include(t => t.FromCashBox)
            .Include(t => t.FromBankAccount)
            .Include(t => t.ToCashBox)
            .Include(t => t.ToBankAccount)
            .FirstOrDefaultAsync(t => t.TransferId == request.TransferId, cancellationToken)
            ?? throw new NotFoundException("Transfer", request.TransferId);

        // Resolve user display names
        var userIds = new[] { (Guid?)transfer.CreatedBy, transfer.ModifiedBy }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var memberNames = await _db.FamilyMembers
            .Where(m => userIds.Contains(m.UserId))
            .Select(m => new { m.UserId, m.DisplayName })
            .ToListAsync(cancellationToken);

        var nameByUserId = memberNames
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => g.First().DisplayName);

        return new TransferDto
        {
            TransferId = transfer.TransferId,
            Date = transfer.Date,
            FromCashBoxId = transfer.FromCashBoxId,
            FromCashBoxName = transfer.FromCashBox?.Name,
            FromBankAccountId = transfer.FromBankAccountId,
            FromBankAccountName = transfer.FromBankAccount?.Name,
            ToCashBoxId = transfer.ToCashBoxId,
            ToCashBoxName = transfer.ToCashBox?.Name,
            ToBankAccountId = transfer.ToBankAccountId,
            ToBankAccountName = transfer.ToBankAccount?.Name,
            FromCurrencyCode = transfer.FromCashBox?.CurrencyCode ?? transfer.FromBankAccount?.CurrencyCode ?? string.Empty,
            ToCurrencyCode = transfer.ToCashBox?.CurrencyCode ?? transfer.ToBankAccount?.CurrencyCode ?? string.Empty,
            Amount = transfer.Amount,
            ExchangeRate = transfer.ExchangeRate,
            FromPrimaryExchangeRate = transfer.FromPrimaryExchangeRate,
            FromSecondaryExchangeRate = transfer.FromSecondaryExchangeRate,
            ToPrimaryExchangeRate = transfer.ToPrimaryExchangeRate,
            ToSecondaryExchangeRate = transfer.ToSecondaryExchangeRate,
            AmountTo = transfer.AmountTo,
            AmountToInPrimary = transfer.AmountToInPrimary,
            AmountToInSecondary = transfer.AmountToInSecondary,
            AmountInPrimary = transfer.AmountInPrimary,
            AmountInSecondary = transfer.AmountInSecondary,
            Description = transfer.Description,
            RowVersion = transfer.RowVersion,
            Status = transfer.Status.ToString(),
            IsAutoConfirmed = transfer.IsAutoConfirmed,
            RejectionComment = transfer.RejectionComment,
            CreatorUserId = transfer.CreatedBy.ToString(),
            CreatedAt = transfer.CreatedAt,
            CreatedByName = nameByUserId.GetValueOrDefault(transfer.CreatedBy),
            ModifiedAt = transfer.ModifiedAt,
            ModifiedByName = transfer.ModifiedBy.HasValue ? nameByUserId.GetValueOrDefault(transfer.ModifiedBy.Value) : null,
        };
    }
}
