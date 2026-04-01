using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.Movements.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.Movements.Queries;

public class GetMovementByIdQueryHandler : IRequestHandler<GetMovementByIdQuery, MovementDto>
{
    private readonly IApplicationDbContext _db;

    public GetMovementByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<MovementDto> Handle(GetMovementByIdQuery request, CancellationToken cancellationToken)
    {
        var movement = await _db.Movements
            .Include(m => m.Payments)
            .FirstOrDefaultAsync(m => m.MovementId == request.MovementId, cancellationToken)
            ?? throw new NotFoundException("Movement", request.MovementId);

        var userIds = new[] { (Guid?)movement.CreatedBy, movement.ModifiedBy }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var nameByUserId = await _db.FamilyMembers
            .Where(m => userIds.Contains(m.UserId))
            .Select(m => new { m.UserId, m.DisplayName })
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.GroupBy(m => m.UserId).ToDictionary(g => g.Key, g => g.First().DisplayName));

        // Check which CC payments have installments assigned to a statement period
        var ccPaymentIds = movement.Payments
            .Where(p => p.PaymentMethodType == PaymentMethodType.CreditCard)
            .Select(p => p.MovementPaymentId)
            .ToList();

        var lockedPaymentIds = ccPaymentIds.Count > 0
            ? (await _db.CreditCardInstallments
                .Where(i => ccPaymentIds.Contains(i.MovementPaymentId)
                    && i.StatementPeriodId != null
                    && i.DeletedAt == null)
                .Select(i => i.MovementPaymentId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet()
            : new HashSet<Guid>();

        return new MovementDto
        {
            MovementId = movement.MovementId,
            Date = movement.Date,
            MovementType = movement.MovementType.ToString(),
            Amount = movement.Amount,
            CurrencyCode = movement.CurrencyCode,
            PrimaryExchangeRate = movement.PrimaryExchangeRate,
            SecondaryExchangeRate = movement.SecondaryExchangeRate,
            AmountInPrimary = movement.AmountInPrimary,
            AmountInSecondary = movement.AmountInSecondary,
            Description = movement.Description,
            SubcategoryId = movement.SubcategoryId,
            AccountingType = movement.AccountingType,
            IsOrdinary = movement.IsOrdinary,
            CostCenterId = movement.CostCenterId,
            Source = movement.Source,
            RowVersion = movement.RowVersion,
            CreatedAt = movement.CreatedAt,
            CreatedByName = nameByUserId.GetValueOrDefault(movement.CreatedBy),
            ModifiedAt = movement.ModifiedAt,
            ModifiedByName = movement.ModifiedBy.HasValue ? nameByUserId.GetValueOrDefault(movement.ModifiedBy.Value) : null,
            Payments = movement.Payments.Select(p => new MovementPaymentDto
            {
                MovementPaymentId = p.MovementPaymentId,
                PaymentMethodType = p.PaymentMethodType.ToString(),
                Amount = p.Amount,
                CashBoxId = p.CashBoxId,
                BankAccountId = p.BankAccountId,
                CreditCardId = p.CreditCardId,
                CreditCardMemberId = p.CreditCardMemberId,
                Installments = p.Installments,
                BonificationType = p.BonificationType?.ToString(),
                BonificationValue = p.BonificationValue,
                BonificationAmount = p.BonificationAmount,
                NetAmount = p.NetAmount,
                HasAssignedInstallments = p.PaymentMethodType == PaymentMethodType.CreditCard
                    && lockedPaymentIds.Contains(p.MovementPaymentId),
            }).ToList(),
        };
    }
}
