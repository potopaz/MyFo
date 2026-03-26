using MediatR;
using MyFO.Application.Admin.DTOs;

namespace MyFO.Application.Admin.Queries;

public class GetAdminFamiliesQuery : IRequest<List<AdminFamilyListItemDto>> { }
