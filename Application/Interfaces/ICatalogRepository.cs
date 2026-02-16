using Domain.Entities;

namespace Application.Interfaces;

public interface ICatalogRepository
{
    Task<IReadOnlyList<CatalogItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CatalogItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CatalogItem> AddAsync(CatalogItem item, CancellationToken cancellationToken = default);
}
