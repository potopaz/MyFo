using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;
using MyFO.Domain.CreditCards.Enums;

namespace MyFO.Application.CreditCards.StatementPeriods.Queries;

public class GetAllStatementPeriodsQueryHandler : IRequestHandler<GetAllStatementPeriodsQuery, List<StatementPeriodDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAllStatementPeriodsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<StatementPeriodDto>> Handle(GetAllStatementPeriodsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.StatementPeriods.AsQueryable();

        if (request.CreditCardId.HasValue)
            query = query.Where(sp => sp.CreditCardId == request.CreditCardId.Value);

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (request.Status == "Open")
                query = query.Where(sp => sp.ClosedAt == null);
            else if (request.Status == "Closed")
                query = query.Where(sp => sp.ClosedAt != null);
            else if (Enum.TryParse<PaymentStatus>(request.Status, out var paymentStatus))
                query = query.Where(sp => sp.PaymentStatus == paymentStatus);
        }

        return await query
            .OrderBy(sp => sp.PeriodEnd)
            .Join(_db.CreditCards,
                sp => new { sp.FamilyId, sp.CreditCardId },
                cc => new { cc.FamilyId, cc.CreditCardId },
                (sp, cc) => new StatementPeriodDto
                {
                    StatementPeriodId = sp.StatementPeriodId,
                    CreditCardId = sp.CreditCardId,
                    CreditCardName = cc.Name,
                    PeriodEnd = sp.PeriodEnd,
                    DueDate = sp.DueDate,
                    PaymentStatus = sp.PaymentStatus.ToString(),
                    PreviousBalance = sp.PreviousBalance,
                    InstallmentsTotal = sp.InstallmentsTotal,
                    ChargesTotal = sp.ChargesTotal,
                    BonificationsTotal = sp.BonificationsTotal,
                    StatementTotal = sp.StatementTotal,
                    PaymentsTotal = sp.PaymentsTotal,
                    PendingBalance = sp.PendingBalance,
                    ClosedAt = sp.ClosedAt,
                })
            .ToListAsync(cancellationToken);
    }
}
