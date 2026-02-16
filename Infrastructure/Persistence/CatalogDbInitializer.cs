using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class CatalogDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CatalogDbInitializer");

        var dbConnection = dbContext.Database.GetDbConnection();
        if (!string.IsNullOrWhiteSpace(dbConnection.DataSource))
        {
            var dbPath = Path.GetFullPath(dbConnection.DataSource);
            var dataDirectory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.CatalogItems.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.CatalogItems.AddRange(
            new CatalogItem
            {
                Name = "Noise Cancelling Headphones",
                Description = "Wireless over-ear headphones with ANC",
                Category = "Electronics",
                Price = 199.99m,
                QuantityInStock = 25
            },
            new CatalogItem
            {
                Name = "Ergonomic Desk Chair",
                Description = "Lumbar support office chair",
                Category = "Furniture",
                Price = 349.00m,
                QuantityInStock = 10
            },
            new CatalogItem
            {
                Name = "Premium Notebook Set",
                Description = "Pack of 5 ruled notebooks",
                Category = "Office Supplies",
                Price = 24.95m,
                QuantityInStock = 100
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("SQLite database initialized with sample catalog data.");
    }
}
