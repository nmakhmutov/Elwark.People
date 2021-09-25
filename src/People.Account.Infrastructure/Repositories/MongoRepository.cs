using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using People.Account.Domain.Seed;
using People.Mongo;

namespace People.Account.Infrastructure.Repositories
{
    public abstract class MongoRepository<TEntity, TKey> : IRepository<TEntity, TKey>
        where TEntity : Entity<TKey>, IAggregateRoot
        where TKey : struct
    {
        private readonly IClientSessionHandle? _session;
        protected readonly IMongoCollection<TEntity> Collection;

        protected MongoRepository(IMongoCollection<TEntity> collection, IClientSessionHandle? session)
        {
            Collection = collection;
            _session = session;
        }

        public async Task<bool> ExistsAsync(TKey id, CancellationToken ct)
        {
            var filter = Builders<TEntity>.Filter.Eq(x => x.Id, id);
            return await Collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, ct) > 0;
        }

        public async Task<TEntity?> GetAsync(TKey key, CancellationToken ct) =>
            await Collection.Find(Builders<TEntity>.Filter.Eq(x => x.Id, key)).FirstOrDefaultAsync(ct);

        public async Task<List<TEntity>> GetAsync(Expression<Func<TEntity, bool>> criteria, CancellationToken ct) =>
            await Collection.AsQueryable().Where(criteria).ToListAsync(ct);

        public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (entity is IHasHistory history)
                history.SetAsCreated(DateTime.UtcNow);

            if (_session is null)
                await Collection.InsertOneAsync(entity, new InsertOneOptions(), ct);
            else
                await Collection.InsertOneAsync(_session, entity, new InsertOneOptions(), ct);

            return entity;
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Eq(x => x.Id, entity.Id),
                Builders<TEntity>.Filter.Eq(x => x.Version, entity.Version)
            );

            entity.Version = (entity.Version == int.MaxValue ? int.MinValue : entity.Version) + 1;

            if (entity is IHasHistory history)
                history.SetAsUpdated(DateTime.UtcNow);

            var result = _session is null
                ? await Collection.ReplaceOneAsync(filter, entity, new ReplaceOptions(), ct)
                : await Collection.ReplaceOneAsync(_session, filter, entity, new ReplaceOptions(), ct);

            if (result.ModifiedCount == 0)
                throw new MongoUpdateException($"Entity with id '{entity.Id}' not updated");
        }

        public Task DeleteAsync(TKey key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            
            var filter = Builders<TEntity>.Filter.Eq(x => x.Id, key);

            return _session is null
                ? Collection.DeleteOneAsync(filter, new DeleteOptions(), ct)
                : Collection.DeleteOneAsync(_session, filter, new DeleteOptions(), ct);
        }
    }
}
