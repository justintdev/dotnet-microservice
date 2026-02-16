namespace Application.Models;

public sealed class CreateCatalogItemRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int QuantityInStock { get; init; }
}
