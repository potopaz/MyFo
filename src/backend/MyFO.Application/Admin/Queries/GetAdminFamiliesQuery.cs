using MyFO.Application.Common.Mediator;
using MyFO.Application.Admin.DTOs;

namespace MyFO.Application.Admin.Queries;

public class GetAdminFamiliesQuery : IRequest<List<AdminFamilyListItemDto>> { }
