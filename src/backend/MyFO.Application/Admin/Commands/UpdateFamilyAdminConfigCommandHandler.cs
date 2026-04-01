using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Admin.DTOs;
using MyFO.Application.Admin.Queries;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Identity;

namespace MyFO.Application.Admin.Commands;

public class UpdateFamilyAdminConfigCommandHandler : IRequestHandler<UpdateFamilyAdminConfigCommand, AdminFamilyDetailDto>
{
    private readonly IAdminDbContext _db;
    private readonly IMediator _mediator;

    public UpdateFamilyAdminConfigCommandHandler(IAdminDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<AdminFamilyDetailDto> Handle(UpdateFamilyAdminConfigCommand request, CancellationToken cancellationToken)
    {
        var familyExists = await _db.Families
                        .AnyAsync(f => f.FamilyId == request.FamilyId && f.DeletedAt == null, cancellationToken);

        if (!familyExists)
            throw new NotFoundException("Family", request.FamilyId);

        var config = await _db.FamilyAdminConfigs
                        .FirstOrDefaultAsync(c => c.FamilyId == request.FamilyId && c.DeletedAt == null, cancellationToken);

        if (config is null)
        {
            config = new FamilyAdminConfig
            {
                FamilyAdminConfigId = Guid.NewGuid(),
                FamilyId = request.FamilyId,
            };
            await _db.FamilyAdminConfigs.AddAsync(config, cancellationToken);
        }

        var wasEnabled = config.IsEnabled;

        config.IsEnabled = request.IsEnabled;
        config.MaxMembers = request.MaxMembers;
        config.Notes = request.Notes;
        config.DisabledReason = request.IsEnabled ? null : request.DisabledReason;

        if (!request.IsEnabled && wasEnabled)
            config.DisabledAt = DateTime.UtcNow;
        else if (request.IsEnabled && !wasEnabled)
            config.DisabledAt = null;

        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetAdminFamilyDetailQuery { FamilyId = request.FamilyId }, cancellationToken);
    }
}
