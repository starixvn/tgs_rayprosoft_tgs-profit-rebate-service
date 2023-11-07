using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Repository;

namespace Tgs.ProfitRebate.Core;

public interface ITgsService<T>
{
    ITgsRepository Repository { get; }

    IQueryable<T> GetQueries(
        Func<IQueryable<T>, IQueryable<T>>? preQuery = null,
        bool tracking = false);

    ValueTask CreateEntityAsync(T entity);

    ValueTask CreateEntitiesAsync(T[] entities);

    ValueTask UpdateEntitiesAsync(T[] entities);
}

public class TgsService<T> : ITgsService<T> where T : class
{
    private ITgsRepository Repository { get; }

    public TgsService(ITgsRepository repository)
    {
        Repository = repository;
    }

    ITgsRepository ITgsService<T>.Repository => Repository;

    public async ValueTask CreateEntityAsync(T entity)
    {
        _ = await Repository.Set<T>().AddAsync(entity);
        _ = await Repository.SaveChangesAsync();
    }

    public async ValueTask CreateEntitiesAsync(T[] entities)
    {
        await Repository.Set<T>().AddRangeAsync(entities);
        _ = await Repository.SaveChangesAsync();
    }

    public IQueryable<T> GetQueries(Func<IQueryable<T>, IQueryable<T>>? preQuery, bool tracking)
    {
        preQuery ??= c => c;
        return tracking ? preQuery(Repository.Set<T>()) : preQuery(Repository.Set<T>()).AsNoTracking();
    }

    public async ValueTask UpdateEntitiesAsync(T[] entities)
    {
        Repository.Set<T>().UpdateRange(entities);
        _ = await Repository.SaveChangesAsync();
    }
}