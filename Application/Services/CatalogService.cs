using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class CatalogService : ICatalogService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly ICatalogRepository _catalogRepository;
    private readonly IAppCache _cache;
    private readonly IEventPublisher _eventPublisher;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(
        ICatalogRepository catalogRepository,
        IAppCache cache,
        IEventPublisher eventPublisher,
        IFeatureFlagService featureFlagService,
        ILogger<CatalogService> logger)
    {
        _catalogRepository = catalogRepository;
        _cache = cache;
        _eventPublisher = eventPublisher;
        _featureFlagService = featureFlagService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CatalogItemDto>> GetCatalogItemsAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "catalog:items:all";
        var useCaching = await _featureFlagService.IsEnabledAsync(FeatureFlags.EnableRedisCaching, cancellationToken);

        if (useCaching)
        {
            var cachedItems = await _cache.GetAsync<IReadOnlyList<CatalogItemDto>>(cacheKey, cancellationToken);
            if (cachedItems is not null)
            {
                return cachedItems;
            }
        }

        var items = await _catalogRepository.GetAllAsync(cancellationToken);
        var mapped = items.Select(Map).ToList();

        if (useCaching)
        {
            await _cache.SetAsync(cacheKey, mapped, CacheTtl, cancellationToken);
        }

        return mapped;
    }

    public async Task<CatalogItemDto?> GetCatalogItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _catalogRepository.GetByIdAsync(id, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<CatalogItemDto> CreateCatalogItemAsync(CreateCatalogItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = new CatalogItem
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Price = request.Price,
            QuantityInStock = request.QuantityInStock
        };

        var saved = await _catalogRepository.AddAsync(item, cancellationToken);

        if (await _featureFlagService.IsEnabledAsync(FeatureFlags.EnableRedisCaching, cancellationToken))
        {
            await _cache.RemoveAsync("catalog:items:all", cancellationToken);
        }

        if (await _featureFlagService.IsEnabledAsync(FeatureFlags.EnableKafkaPublishing, cancellationToken))
        {
            await _eventPublisher.PublishCatalogItemCreatedAsync(saved, cancellationToken);
        }

        _logger.LogInformation("Catalog item created. CatalogItemId={CatalogItemId}", saved.Id);

        return Map(saved);
    }

    private static CatalogItemDto Map(CatalogItem item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Category = item.Category,
            Price = item.Price,
            QuantityInStock = item.QuantityInStock,
            CreatedUtc = item.CreatedUtc
        };
}
