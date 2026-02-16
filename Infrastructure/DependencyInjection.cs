using Application.Interfaces;
using Application.Services;
using Confluent.Kafka;
using Infrastructure.Caching;
using Infrastructure.Configuration;
using Infrastructure.FeatureFlags;
using Infrastructure.Health;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var sqliteConnection = configuration.GetConnectionString("CatalogDatabase")
            ?? "Data Source=data/catalog.db";
        var redisCachingEnabled = configuration.GetValue<bool>("FeatureManagement:EnableRedisCaching");
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        services.AddDbContext<CatalogDbContext>(options => options.UseSqlite(sqliteConnection));
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<ICatalogService, CatalogService>();

        if (redisCachingEnabled)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
            });
            services.AddScoped<IAppCache, RedisAppCache>();
        }
        else
        {
            services.AddScoped<IAppCache, NoOpAppCache>();
        }
        services.AddScoped<IEventPublisher, KafkaCatalogEventProducer>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();

        services.AddSingleton<IProducer<Null, string>>(serviceProvider =>
        {
            var kafka = serviceProvider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafka.BootstrapServers,
                Acks = Acks.All
            };

            return new ProducerBuilder<Null, string>(producerConfig).Build();
        });

        services.AddHostedService<KafkaCatalogEventsConsumer>();

        var healthChecks = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
            .AddCheck<SqliteReadyHealthCheck>("sqlite", tags: new[] { "ready" });

        if (redisCachingEnabled)
        {
            healthChecks.AddCheck<RedisReadyHealthCheck>("redis", tags: new[] { "ready" });
        }

        return services;
    }
}
