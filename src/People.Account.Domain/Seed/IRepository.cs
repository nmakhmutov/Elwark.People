using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace People.Account.Domain.Seed
{
    public interface IRepository<TEntity, in TKey>
        where TEntity : Entity<TKey>, IAggregateRoot
        where TKey : struct
    {
        Task<bool> ExistsAsync(TKey id, CancellationToken ct = default);

        public Task<TEntity?> GetAsync(TKey key, CancellationToken ct = default);

        public Task<List<TEntity>> GetAsync(Expression<Func<TEntity, bool>> criteria, CancellationToken ct = default);

        public Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default);

        public Task UpdateAsync(TEntity entity, CancellationToken ct = default);

        public Task DeleteAsync(TKey key, CancellationToken ct = default);
    }
}
