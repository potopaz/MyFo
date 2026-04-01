using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.CreditCardPayments.DTOs;

namespace MyFO.Application.CreditCards.CreditCardPayments.Queries;

public class GetCreditCardPaymentsQueryHandler : IRequestHandler<GetCreditCardPaymentsQuery, List<CreditCardPaymentDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCreditCardPaymentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CreditCardPaymentDto>> Handle(GetCreditCardPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.CreditCardPayments
            .Include(p => p.CreditCard)
            .Include(p => p.StatementPeriod)
            .AsQueryable();

        if (request.CreditCardId.HasValue)
            query = query.Where(p => p.CreditCardId == request.CreditCardId.Value);

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

        // Load source names
        var cashBoxIds = payments.Where(p => p.CashBoxId.HasValue).Select(p => p.CashBoxId!.Value).Distinct().ToList();
        var bankIds = payments.Where(p => p.BankAccountId.HasValue).Select(p => p.BankAccountId!.Value).Distinct().ToList();

        var cashBoxNames = cashBoxIds.Count > 0
            ? await _db.CashBoxes.Where(cb => cashBoxIds.Contains(cb.CashBoxId))
                .ToDictionaryAsync(cb => cb.CashBoxId, cb => cb.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var bankNames = bankIds.Count > 0
            ? await _db.BankAccounts.Where(ba => bankIds.Contains(ba.BankAccountId))
                .ToDictionaryAsync(ba => ba.BankAccountId, ba => ba.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return payments.Select(p => new CreditCardPaymentDto
        {
            CreditCardPaymentId = p.CreditCardPaymentId,
            CreditCardId = p.CreditCardId,
            CreditCardName = p.CreditCard.Name,
            PaymentDate = p.PaymentDate,
            Amount = p.Amount,
            Description = p.Description,
            CashBoxId = p.CashBoxId,
            CashBoxName = p.CashBoxId.HasValue && cashBoxNames.TryGetValue(p.CashBoxId.Value, out var cbName) ? cbName : null,
            BankAccountId = p.BankAccountId,
            BankAccountName = p.BankAccountId.HasValue && bankNames.TryGetValue(p.BankAccountId.Value, out var baName) ? baName : null,
            IsTotalPayment = p.IsTotalPayment,
            StatementPeriodId = p.StatementPeriodId,
            IsPeriodClosed = p.StatementPeriod != null && p.StatementPeriod.ClosedAt != null,
            PrimaryExchangeRate = p.PrimaryExchangeRate,
            SecondaryExchangeRate = p.SecondaryExchangeRate,
            AmountInPrimary = p.AmountInPrimary,
            AmountInSecondary = p.AmountInSecondary,
        }).ToList();
    }
}
