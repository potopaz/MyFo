using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.Transfers.Commands;

public class RejectTransferCommandHandler : IRequestHandler<RejectTransferCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RejectTransferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RejectTransferCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var transfer = await _db.Transfers
            .FirstOrDefaultAsync(t => t.FamilyId == familyId && t.TransferId == request.TransferId, cancellationToken)
            ?? throw new NotFoundException("Transfer", request.TransferId);

        if (transfer.Status != TransferStatus.PendingConfirmation)
            throw new DomainException("INVALID_STATUS", "Solo se pueden rechazar transferencias en estado pendiente.");

        transfer.Status = TransferStatus.Rejected;
        transfer.RejectionComment = request.Comment?.Trim();

        await _db.SaveChangesAsync(cancellationToken);
    }
}
