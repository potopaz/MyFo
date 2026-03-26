using MediatR;
using MyFO.Application.Admin.DTOs;

namespace MyFO.Application.Admin.Commands;

public class UpdateFamilyAdminConfigCommand : IRequest<AdminFamilyDetailDto>
{
    public Guid FamilyId { get; set; }
    public bool IsEnabled { get; set; }
    public int? MaxMembers { get; set; }
    public string? Notes { get; set; }
    public string? DisabledReason { get; set; }
}
