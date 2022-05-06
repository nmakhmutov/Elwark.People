using System.Threading.Tasks;
using Common.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using People.Infrastructure.Blacklist;
using People.Infrastructure.Countries;

namespace People.Infrastructure;

public sealed class InfrastructureDbContext : MongoDbContext
{
    static InfrastructureDbContext() =>
        BsonClassMap.RegisterClassMap<Country>(map =>
        {
            map.AutoMap();
            map.MapIdProperty(x => x.Alpha2Code);
        });

    public InfrastructureDbContext(IOptions<MongoDbOptions> settings)
        : base(settings.Value)
    {
    }

    public IMongoCollection<Country> Countries =>
        Database.GetCollection<Country>("countries");

    public IMongoCollection<BlacklistItem> Blacklist =>
        Database.GetCollection<BlacklistItem>("blacklist");

    public override async Task OnModelCreatingAsync()
    {
        await CreateCollectionsAsync(
            Countries.CollectionNamespace.CollectionName,
            Blacklist.CollectionNamespace.CollectionName
        );

        await CreateIndexesAsync(Countries,
            new CreateIndexModel<Country>(
                Builders<Country>.IndexKeys.Ascending(x => x.Alpha3Code),
                new CreateIndexOptions { Name = "Alpha3Code", Unique = true }
            )
        );

        await CreateIndexesAsync(Blacklist,
            new CreateIndexModel<BlacklistItem>(
                Builders<BlacklistItem>.IndexKeys.Combine(
                    Builders<BlacklistItem>.IndexKeys.Ascending(x => x.Type),
                    Builders<BlacklistItem>.IndexKeys.Ascending(x => x.Value)
                ),
                new CreateIndexOptions { Name = "Type_Value", Unique = true }
            )
        );
    }
}
