using Infrastructure.Caching;

namespace Microservice.Tests;

public class NoOpAppCacheTests
{
    [Fact]
    public async Task GetAsync_ReturnsDefaultValue()
    {
        var cache = new NoOpAppCache();

        var result = await cache.GetAsync<string>("missing-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_And_RemoveAsync_DoNotThrow()
    {
        var cache = new NoOpAppCache();

        await cache.SetAsync("key", "value", TimeSpan.FromMinutes(1));
        await cache.RemoveAsync("key");
    }
}
