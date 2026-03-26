using MediatR;

namespace MyFO.Application.Accounting.Categories.Commands;

public record DeleteCategoryCommand(Guid CategoryId) : IRequest;
