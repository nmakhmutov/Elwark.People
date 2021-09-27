using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using People.Mongo;
using People.Notification.Api.Models;

namespace People.Notification.Api.Infrastructure.Repositories
{
    internal sealed class EmailProviderRepository : IEmailProviderRepository
    {
        private readonly NotificationDbContext _dbContext;

        public EmailProviderRepository(NotificationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EmailProvider?> GetNextAsync(CancellationToken ct)
        {
            var filter = Builders<EmailProvider>.Filter.And(
                Builders<EmailProvider>.Filter.Gt(x => x.Balance, 0),
                Builders<EmailProvider>.Filter.Eq(x => x.IsEnabled, true)
            );

            return await _dbContext.EmailProviders
                .Find(filter)
                .SortByDescending(x => x.Balance)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<EmailProvider?> GetAsync(EmailProvider.Type key, CancellationToken ct) =>
            await _dbContext.EmailProviders
                .Find(Builders<EmailProvider>.Filter.Eq(x => x.Id, key))
                .FirstOrDefaultAsync(ct);

        public async Task<EmailProvider> CreateAsync(EmailProvider entity, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            entity.Version++;
            await _dbContext.EmailProviders.InsertOneAsync(entity, new InsertOneOptions(), ct);

            return entity;
        }

        public async Task UpdateAsync(EmailProvider entity, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var filter = Builders<EmailProvider>.Filter.And(
                Builders<EmailProvider>.Filter.Eq(x => x.Id, entity.Id),
                Builders<EmailProvider>.Filter.Eq(x => x.Version, entity.Version)
            );

            entity.Version = (entity.Version == int.MaxValue ? int.MinValue : entity.Version) + 1;

            var result = await _dbContext.EmailProviders.ReplaceOneAsync(filter, entity, new ReplaceOptions(), ct);

            if (result.ModifiedCount == 0)
                throw new MongoUpdateException($"Entity with id '{entity.Id}' not updated");
        }

        public Task DeleteAsync(EmailProvider.Type key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var filter = Builders<EmailProvider>.Filter.Eq(x => x.Id, key);

            return _dbContext.EmailProviders.DeleteOneAsync(filter, new DeleteOptions(), ct);
        }
    }
}
