using Domain.Entities;

namespace Application.Interfaces;

public interface IEventPublisher
{
    Task PublishCatalogItemCreatedAsync(CatalogItem item, CancellationToken cancellationToken = default);
}
