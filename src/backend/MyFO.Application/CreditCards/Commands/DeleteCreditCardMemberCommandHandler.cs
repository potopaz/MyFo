using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.Commands;

public class DeleteCreditCardMemberCommandHandler : IRequestHandler<DeleteCreditCardMemberCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCreditCardMemberCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCreditCardMemberCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var member = await _db.CreditCardMembers
            .FirstOrDefaultAsync(m => m.FamilyId == familyId
                && m.CreditCardId == request.CreditCardId
                && m.CreditCardMemberId == request.CreditCardMemberId, cancellationToken)
            ?? throw new NotFoundException("CreditCardMember", request.CreditCardMemberId);

        member.DeletedAt = DateTime.UtcNow;
        member.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
