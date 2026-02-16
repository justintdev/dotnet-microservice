using Application.Models;

namespace Application.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyList<CatalogItemDto>> GetCatalogItemsAsync(CancellationToken cancellationToken = default);
    Task<CatalogItemDto?> GetCatalogItemByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CatalogItemDto> CreateCatalogItemAsync(CreateCatalogItemRequest request, CancellationToken cancellationToken = default);
}
