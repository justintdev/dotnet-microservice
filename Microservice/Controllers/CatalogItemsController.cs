using Application.Interfaces;
using Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Microservice.Controllers;

[ApiController]
[Authorize]
[Route("api/catalog-items")]
public sealed class CatalogItemsController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogItemsController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CatalogItemDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await _catalogService.GetCatalogItemsAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CatalogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _catalogService.GetCatalogItemByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CatalogItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CatalogItemDto>> CreateAsync([FromBody] CreateCatalogItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Category) || request.Price < 0 || request.QuantityInStock < 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid catalog item payload.",
                Detail = "Name/category are required. Price and quantity must be non-negative."
            });
        }

        var created = await _catalogService.CreateCatalogItemAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }
}
