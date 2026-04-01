using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.Transfers.DTOs;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.Transfers.Queries;

public class GetTransfersQueryHandler : IRequestHandler<GetTransfersQuery, List<TransferListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTransfersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<TransferListItemDto>> Handle(GetTransfersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Transfers.AsQueryable();

        if (request.DateFrom.HasValue)
            query = query.Where(t => t.Date >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(t => t.Date <= request.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<TransferStatus>(request.Status, ignoreCase: true, out var statusFilter))
        {
            query = query.Where(t => t.Status == statusFilter);
        }

        var transfers = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Include(t => t.FromCashBox)
            .Include(t => t.FromBankAccount)
            .Include(t => t.ToCashBox)
            .Include(t => t.ToBankAccount)
            .ToListAsync(cancellationToken);

        // Resolve user names via FamilyMembers (UserId → DisplayName)
        var userIds = transfers
            .SelectMany(t => new[] { (Guid?)t.CreatedBy, t.ModifiedBy })
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

        return transfers.Select(t => new TransferListItemDto
        {
            TransferId = t.TransferId,
            Date = t.Date,
            FromCashBoxId = t.FromCashBoxId,
            FromCashBoxName = t.FromCashBox?.Name,
            FromBankAccountId = t.FromBankAccountId,
            FromBankAccountName = t.FromBankAccount?.Name,
            ToCashBoxId = t.ToCashBoxId,
            ToCashBoxName = t.ToCashBox?.Name,
            ToBankAccountId = t.ToBankAccountId,
            ToBankAccountName = t.ToBankAccount?.Name,
            FromCurrencyCode = t.FromCashBox?.CurrencyCode ?? t.FromBankAccount?.CurrencyCode ?? string.Empty,
            ToCurrencyCode = t.ToCashBox?.CurrencyCode ?? t.ToBankAccount?.CurrencyCode ?? string.Empty,
            Amount = t.Amount,
            ExchangeRate = t.ExchangeRate,
            FromPrimaryExchangeRate = t.FromPrimaryExchangeRate,
            FromSecondaryExchangeRate = t.FromSecondaryExchangeRate,
            ToPrimaryExchangeRate = t.ToPrimaryExchangeRate,
            ToSecondaryExchangeRate = t.ToSecondaryExchangeRate,
            AmountTo = t.AmountTo,
            AmountToInPrimary = t.AmountToInPrimary,
            AmountToInSecondary = t.AmountToInSecondary,
            AmountInPrimary = t.AmountInPrimary,
            AmountInSecondary = t.AmountInSecondary,
            Description = t.Description,
            RowVersion = t.RowVersion,
            CreatedAt = t.CreatedAt,
            CreatedByName = nameByUserId.GetValueOrDefault(t.CreatedBy),
            ModifiedAt = t.ModifiedAt,
            ModifiedByName = t.ModifiedBy.HasValue ? nameByUserId.GetValueOrDefault(t.ModifiedBy.Value) : null,
            Status = t.Status.ToString(),
            IsAutoConfirmed = t.IsAutoConfirmed,
            RejectionComment = t.RejectionComment,
            CreatorUserId = t.CreatedBy.ToString(),
        }).ToList();
    }
}
