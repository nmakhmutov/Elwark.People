using System.Threading;
using System.Threading.Tasks;

namespace People.Domain.SeedWork
{
    public interface IRepository<in TKey, TEntity> 
        where TEntity : Entity<TKey>, IAggregateRoot 
        where TKey : struct
    {
        public Task<TEntity?> GetAsync(TKey id, CancellationToken ct = default);

        public Task CreateAsync(TEntity entity, CancellationToken ct = default);

        public Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    }
}