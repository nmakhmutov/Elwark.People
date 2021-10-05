using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;

namespace People.Infrastructure.Repositories;

internal sealed class AccountRepository
    : MongoRepository<Account, AccountId>, IAccountRepository
{
    public AccountRepository(PeopleDbContext dbContext)
        : base(dbContext.Accounts, dbContext.Session)
    {
    }

    public async Task<Account?> GetAsync(Identity key, CancellationToken ct)
    {
        var filter = Builders<Account>.Filter
            .ElemMatch(nameof(Account.Connections),
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
        var filter = Builders<Account>.Filter
            .ElemMatch($"{nameof(Account.Connections)}",
                Builders<Connection>.Filter.And(
                    Builders<Connection>.Filter.Eq(x => x.Type, key.Type),
                    Builders<Connection>.Filter.Eq(x => x.Value, key.Value)
                )
            );

        return await Collection
            .CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, ct) > 0;
    }
}
