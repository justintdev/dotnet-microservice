namespace Application.Models;

public sealed class CatalogItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int QuantityInStock { get; init; }
    public DateTimeOffset CreatedUtc { get; init; }
}
