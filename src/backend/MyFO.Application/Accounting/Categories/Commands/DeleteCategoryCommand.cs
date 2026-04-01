using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Accounting.Categories.Commands;

public record DeleteCategoryCommand(Guid CategoryId) : IRequest;
