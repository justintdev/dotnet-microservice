using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CatalogRepository : ICatalogRepository
{
    private readonly CatalogDbContext _dbContext;

    public CatalogRepository(CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CatalogItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CatalogItems
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<CatalogItem> AddAsync(CatalogItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.CatalogItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }
}
