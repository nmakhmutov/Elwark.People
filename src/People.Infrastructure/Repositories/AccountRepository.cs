using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Driver;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;
using People.Infrastructure.Mongo;

namespace People.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly PeopleDbContext _dbContext;
        private readonly IMediator _mediator;

        public AccountRepository(PeopleDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task<Account?> GetAsync(AccountId id, CancellationToken ct) =>
            await _dbContext.Accounts
                .Find(Builders<Account>.Filter.Eq(x => x.Id, id))
                .FirstOrDefaultAsync(ct);

        public async Task CreateAsync(Account entity, CancellationToken ct)
        {
            entity.Version++;

            await _dbContext.Accounts.InsertOneAsync(entity, new InsertOneOptions(), ct);

            if (entity.DomainEvents.Count > 0)
            {
                await Task.WhenAll(entity.DomainEvents.Select(x => _mediator.Publish(x, ct)));
                entity.ClearDomainEvents();
            }
        }

        public async Task UpdateAsync(Account entity, CancellationToken ct)
        {
            var filter = Builders<Account>.Filter.And(
                Builders<Account>.Filter.Eq(x => x.Id, entity.Id),
                Builders<Account>.Filter.Eq(x => x.Version, entity.Version)
            );

            entity.Version = (entity.Version == int.MaxValue ? int.MinValue : entity.Version) + 1;

            var result = await _dbContext.Accounts
                .ReplaceOneAsync(filter, entity, new ReplaceOptions {IsUpsert = false}, ct);

            if (result.ModifiedCount == 0)
                throw new MongoUpdateException($"Account '{entity.Id}' not updated");
            
            if (entity.DomainEvents.Count > 0)
            {
                await Task.WhenAll(entity.DomainEvents.Select(x => _mediator.Publish(x, ct)));
                entity.ClearDomainEvents();
            }
        }

        public async Task<Account?> GetAsync(Identity key, CancellationToken ct)
        {
            var filter = Builders<Account>.Filter
                .ElemMatch($"{nameof(Account.Connections)}",
                    Builders<Connection>.Filter.And(
                        Builders<Connection>.Filter.Eq(x => x.IdentityType, key.Type),
                        Builders<Connection>.Filter.Eq(x => x.Value, key.Value)
                    )
                );

            return await _dbContext.Accounts.Find(filter)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> IsExists(Identity key, CancellationToken ct)
        {
            var filter = Builders<Account>.Filter
                .ElemMatch($"{nameof(Account.Connections)}",
                    Builders<Connection>.Filter.And(
                        Builders<Connection>.Filter.Eq(x => x.IdentityType, key.Type),
                        Builders<Connection>.Filter.Eq(x => x.Value, key.Value)
                    )
                );

            return await _dbContext.Accounts
                .CountDocumentsAsync(filter, new CountOptions{Limit = 1}, ct) > 0;
        }
    }
}
