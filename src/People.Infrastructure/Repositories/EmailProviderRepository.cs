using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Driver;
using People.Domain.AggregateModels.EmailProvider;
using People.Infrastructure.Mongo;

namespace People.Infrastructure.Repositories
{
    public sealed class EmailProviderRepository : IEmailProviderRepository
    {
        private readonly PeopleDbContext _dbContext;
        private readonly IMediator _mediator;

        public EmailProviderRepository(PeopleDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task<EmailProvider?> GetAsync(EmailProviderType id, CancellationToken ct) =>
            await _dbContext.EmailProviders.Find(Builders<EmailProvider>.Filter.Eq(x => x.Id, id))
                .FirstOrDefaultAsync(ct);

        public async Task CreateAsync(EmailProvider entity, CancellationToken ct)
        {
            entity.Version++;
            await _dbContext.EmailProviders.InsertOneAsync(entity, new InsertOneOptions(), ct);

            if (entity.DomainEvents.Count > 0)
            {
                await Task.WhenAll(entity.DomainEvents.Select(x => _mediator.Publish(x, ct)));
                entity.ClearDomainEvents();
            }
        }

        public async Task UpdateAsync(EmailProvider entity, CancellationToken ct)
        {
            var filter = Builders<EmailProvider>.Filter.And(
                Builders<EmailProvider>.Filter.Eq(x => x.Id, entity.Id),
                Builders<EmailProvider>.Filter.Eq(x => x.Version, entity.Version)
            );

            entity.Version = (entity.Version == int.MaxValue ? int.MinValue : entity.Version) + 1;

            var result = await _dbContext.EmailProviders
                .ReplaceOneAsync(filter, entity, new ReplaceOptions {IsUpsert = false}, ct);

            if (result.ModifiedCount == 0)
                throw new MongoUpdateException($"Email provider '{entity.Id}' not updated");

            if (entity.DomainEvents.Count > 0)
            {
                await Task.WhenAll(entity.DomainEvents.Select(x => _mediator.Publish(x, ct)));
                entity.ClearDomainEvents();
            }
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
    }
}
