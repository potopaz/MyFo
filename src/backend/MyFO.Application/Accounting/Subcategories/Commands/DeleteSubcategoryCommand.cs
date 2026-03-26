using MediatR;

namespace MyFO.Application.Accounting.Subcategories.Commands;

public record DeleteSubcategoryCommand(Guid SubcategoryId) : IRequest;
