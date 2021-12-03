using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Repository;

namespace Tgs.ProfitRebate.Core
{
    public interface ITgsService<T>
    {
        ITgsRepository Repository { get; }

        IQueryable<T> GetQueries(
            Func<IQueryable<T>, IQueryable<T>>? preQuery = null,
            bool tracking = false);
        ValueTask CreateEntitiesAsync(T[] entities);
        ValueTask UpdateEntitiesAsync(T[] entities);
    }

    public class TgsService<T> : ITgsService<T> where T : class
    {
        private readonly ITgsRepository _repository;

        public TgsService(ITgsRepository repository)
        {
            _repository = repository;
        }

        ITgsRepository Repository => _repository;

        ITgsRepository ITgsService<T>.Repository => throw new NotImplementedException();

        public async ValueTask CreateEntitiesAsync(T[] entities)
        {
            await _repository.Set<T>().AddRangeAsync(entities);
            await _repository.SaveChangesAsync();
        }

        public IQueryable<T> GetQueries(Func<IQueryable<T>, IQueryable<T>> preQuery, bool tracking)
        {
            preQuery ??= c => c;
            return tracking ? preQuery(_repository.Set<T>()) : preQuery(_repository.Set<T>()).AsNoTracking();
        }

        public async ValueTask UpdateEntitiesAsync(T[] entities)
        {
            _repository.Set<T>().UpdateRange(entities);
            await _repository.SaveChangesAsync();
        }
    }
}