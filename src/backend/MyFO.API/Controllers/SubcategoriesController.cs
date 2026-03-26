using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Accounting.Categories.DTOs;
using MyFO.Application.Accounting.Subcategories.Commands;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/categories/{categoryId}/subcategories")]
public class SubcategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubcategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<SubcategoryDto>> Create(Guid categoryId, [FromBody] CreateSubcategoryCommand command, CancellationToken cancellationToken)
    {
        command.CategoryId = categoryId;
        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/categories/{categoryId}/subcategories/{result.SubcategoryId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SubcategoryDto>> Update(Guid id, [FromBody] UpdateSubcategoryCommand command, CancellationToken cancellationToken)
    {
        command.SubcategoryId = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSubcategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
