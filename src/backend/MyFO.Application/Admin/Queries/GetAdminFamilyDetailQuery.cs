using MediatR;
using MyFO.Application.Admin.DTOs;

namespace MyFO.Application.Admin.Queries;

public class GetAdminFamilyDetailQuery : IRequest<AdminFamilyDetailDto>
{
    public Guid FamilyId { get; set; }
}
