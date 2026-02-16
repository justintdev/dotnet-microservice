using Application;
using Application.Interfaces;
using Application.Models;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microservice.Tests;

public class CatalogServiceTests
{
    [Fact]
    public async Task GetCatalogItemsAsync_ReturnsCachedItems_WhenCachingEnabledAndCacheHit()
    {
        var repository = new Mock<ICatalogRepository>();
        var cache = new Mock<IAppCache>();
        var publisher = new Mock<IEventPublisher>();
        var features = new Mock<IFeatureFlagService>();
        var logger = new Mock<ILogger<CatalogService>>();

        var cached = new List<CatalogItemDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Cached Mouse", Category = "Electronics", Description = "Wireless", Price = 49.99m, QuantityInStock = 5 }
        };

        features.Setup(x => x.IsEnabledAsync(FeatureFlags.EnableRedisCaching, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        cache.Setup(x => x.GetAsync<IReadOnlyList<CatalogItemDto>>("catalog:items:all", It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        var service = new CatalogService(repository.Object, cache.Object, publisher.Object, features.Object, logger.Object);

        var result = await service.GetCatalogItemsAsync();

        Assert.Single(result);
        repository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCatalogItemAsync_PublishesKafkaEvent_WhenFlagEnabled()
    {
        var repository = new Mock<ICatalogRepository>();
        var cache = new Mock<IAppCache>();
        var publisher = new Mock<IEventPublisher>();
        var features = new Mock<IFeatureFlagService>();
        var logger = new Mock<ILogger<CatalogService>>();

        repository.Setup(x => x.AddAsync(It.IsAny<CatalogItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CatalogItem item, CancellationToken _) => item);

        features.Setup(x => x.IsEnabledAsync(FeatureFlags.EnableRedisCaching, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        features.Setup(x => x.IsEnabledAsync(FeatureFlags.EnableKafkaPublishing, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = new CatalogService(repository.Object, cache.Object, publisher.Object, features.Object, logger.Object);

        _ = await service.CreateCatalogItemAsync(new CreateCatalogItemRequest
        {
            Name = "Desk Lamp",
            Description = "LED lamp",
            Category = "Office Supplies",
            Price = 39.99m,
            QuantityInStock = 8
        });

        publisher.Verify(x => x.PublishCatalogItemCreatedAsync(It.IsAny<CatalogItem>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync("catalog:items:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCatalogItemAsync_DoesNotPublishKafkaEvent_WhenFlagDisabled()
    {
        var repository = new Mock<ICatalogRepository>();
        var cache = new Mock<IAppCache>();
        var publisher = new Mock<IEventPublisher>();
        var features = new Mock<IFeatureFlagService>();
        var logger = new Mock<ILogger<CatalogService>>();

        repository.Setup(x => x.AddAsync(It.IsAny<CatalogItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CatalogItem item, CancellationToken _) => item);

        features.Setup(x => x.IsEnabledAsync(FeatureFlags.EnableRedisCaching, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        features.Setup(x => x.IsEnabledAsync(FeatureFlags.EnableKafkaPublishing, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = new CatalogService(repository.Object, cache.Object, publisher.Object, features.Object, logger.Object);

        _ = await service.CreateCatalogItemAsync(new CreateCatalogItemRequest
        {
            Name = "Standing Desk",
            Description = "Adjustable desk",
            Category = "Furniture",
            Price = 499.00m,
            QuantityInStock = 3
        });

        publisher.Verify(x => x.PublishCatalogItemCreatedAsync(It.IsAny<CatalogItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
