using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Health;

public sealed class RedisReadyHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _distributedCache;

    public RedisReadyHealthCheck(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var key = "health:redis:ready";
        var probeValue = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        try
        {
            await _distributedCache.SetStringAsync(
                key,
                probeValue,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) },
                cancellationToken);

            var returnedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            return returnedValue == probeValue
                ? HealthCheckResult.Healthy("Redis is reachable.")
                : HealthCheckResult.Unhealthy("Redis returned an unexpected value.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis readiness check failed.", exception);
        }
    }
}
