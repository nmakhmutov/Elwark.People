using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Countries;
using People.Infrastructure.Forbidden;
using Common.Mongo;

namespace People.Infrastructure;

public sealed class InfrastructureDbContext : MongoDbContext
{
    public InfrastructureDbContext(IOptions<MongoDbOptions> settings)
        : base(settings.Value)
    {
    }

    public IMongoCollection<Country> Countries =>
        Database.GetCollection<Country>("countries");

    public IMongoCollection<ForbiddenItem> ForbiddenItems =>
        Database.GetCollection<ForbiddenItem>("forbidden_items");

    public IMongoCollection<Confirmation> Confirmations =>
        Database.GetCollection<Confirmation>("confirmations");

    public override async Task OnModelCreatingAsync()
    {
        await CreateCollectionsAsync(
            Countries.CollectionNamespace.CollectionName,
            ForbiddenItems.CollectionNamespace.CollectionName,
            Confirmations.CollectionNamespace.CollectionName
        );

        await CreateIndexesAsync(Countries,
            new CreateIndexModel<Country>(
                Builders<Country>.IndexKeys.Ascending(x => x.Alpha2Code),
                new CreateIndexOptions { Name = "Alpha2Code", Unique = true }
            )
        );

        await CreateIndexesAsync(ForbiddenItems,
            new CreateIndexModel<ForbiddenItem>(
                Builders<ForbiddenItem>.IndexKeys.Combine(
                    Builders<ForbiddenItem>.IndexKeys.Ascending(x => x.Type),
                    Builders<ForbiddenItem>.IndexKeys.Ascending(x => x.Value)
                ),
                new CreateIndexOptions { Name = "Type_Value", Unique = true }
            )
        );

        await CreateIndexesAsync(Confirmations,
            new CreateIndexModel<Confirmation>(
                Builders<Confirmation>.IndexKeys.Descending(x => x.ExpireAt),
                new CreateIndexOptions { Name = "ExpireAt", ExpireAfter = TimeSpan.Zero }
            ),
            new CreateIndexModel<Confirmation>(
                Builders<Confirmation>.IndexKeys.Combine(
                    Builders<Confirmation>.IndexKeys.Ascending(x => x.AccountId),
                    Builders<Confirmation>.IndexKeys.Descending(x => x.ExpireAt)
                ),
                new CreateIndexOptions { Name = "AccountId_ExpireAt" }
            )
        );
    }
}
