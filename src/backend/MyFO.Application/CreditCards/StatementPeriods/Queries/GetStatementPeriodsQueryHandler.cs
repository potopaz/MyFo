using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;

namespace MyFO.Application.CreditCards.StatementPeriods.Queries;

public class GetStatementPeriodsQueryHandler : IRequestHandler<GetStatementPeriodsQuery, List<StatementPeriodDto>>
{
    private readonly IApplicationDbContext _db;

    public GetStatementPeriodsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<StatementPeriodDto>> Handle(GetStatementPeriodsQuery request, CancellationToken cancellationToken)
    {
        return await _db.StatementPeriods
            .Where(sp => sp.CreditCardId == request.CreditCardId)
            .OrderByDescending(sp => sp.PeriodEnd)
            .Join(_db.CreditCards,
                sp => new { sp.FamilyId, sp.CreditCardId },
                cc => new { cc.FamilyId, cc.CreditCardId },
                (sp, cc) => new StatementPeriodDto
                {
                    StatementPeriodId = sp.StatementPeriodId,
                    CreditCardId = sp.CreditCardId,
                    CreditCardName = cc.Name,
                    PeriodStart = sp.PeriodStart,
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
