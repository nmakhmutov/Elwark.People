using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;

namespace People.Account.Infrastructure.Repositories
{
    internal sealed class AccountRepository
        : MongoRepository<Domain.Aggregates.AccountAggregate.Account, AccountId>, IAccountRepository
    {
        public AccountRepository(PeopleDbContext dbContext)
            : base(dbContext.Accounts, dbContext.Session)
        {
        }

        public async Task<Domain.Aggregates.AccountAggregate.Account?> GetAsync(Identity key, CancellationToken ct)
        {
            var filter = Builders<Domain.Aggregates.AccountAggregate.Account>.Filter
                .ElemMatch(nameof(Domain.Aggregates.AccountAggregate.Account.Connections),
                    Builders<Connection>.Filter.And(
                        Builders<Connection>.Filter.Eq(x => x.Type, key.Type),
                        Builders<Connection>.Filter.Eq(x => x.Value, key.Value)
                    )
                );

            return await Collection.Find(filter)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> IsExists(Identity key, CancellationToken ct)
        {
            var filter = Builders<Domain.Aggregates.AccountAggregate.Account>.Filter
                .ElemMatch($"{nameof(Domain.Aggregates.AccountAggregate.Account.Connections)}",
                    Builders<Connection>.Filter.And(
                        Builders<Connection>.Filter.Eq(x => x.Type, key.Type),
                        Builders<Connection>.Filter.Eq(x => x.Value, key.Value)
                    )
                );

            return await Collection
                .CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, ct) > 0;
        }
    }
}
