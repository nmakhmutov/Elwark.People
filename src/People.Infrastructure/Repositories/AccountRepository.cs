using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Connections;
using People.Domain.Aggregates.AccountAggregate.Identities;

namespace People.Infrastructure.Repositories;

internal sealed class AccountRepository
    : MongoRepository<Account, AccountId>, IAccountRepository
{
    public AccountRepository(PeopleDbContext dbContext)
        : base(dbContext.Accounts, dbContext.Session)
    {
    }

    public async Task<Account?> GetAsync(Identity identity, CancellationToken ct)
    {
        var filter = Builders<Account>.Filter
            .ElemMatch(nameof(Account.Connections), GetConnectionFilter(identity));

        return await Collection.Find(filter)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> IsExists(Identity identity, CancellationToken ct)
    {
        var filter = Builders<Account>.Filter
            .ElemMatch(nameof(Account.Connections), GetConnectionFilter(identity));

        return await Collection
            .CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, ct) > 0;
    }

    private static FilterDefinition<Connection> GetConnectionFilter(Identity identity) =>
        identity switch
        {
            EmailIdentity =>
                Builders<Connection>.Filter.OfType<EmailConnection>(x => x.Value == identity.Value),

            GoogleIdentity =>
                Builders<Connection>.Filter.OfType<GoogleConnection>(x => x.Value == identity.Value),

            MicrosoftIdentity =>
                Builders<Connection>.Filter.OfType<MicrosoftConnection>(x => x.Value == identity.Value),

            _ => throw new ArgumentOutOfRangeException(nameof(identity))
        };
}
