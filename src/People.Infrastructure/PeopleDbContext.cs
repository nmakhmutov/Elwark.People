using System;
using System.Threading.Tasks;
using Common.Mongo;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Connections;
using People.Domain.Seed;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Sequences;
using People.Infrastructure.Serializers;

namespace People.Infrastructure;

public sealed class PeopleDbContext : MongoDbContext
{
    static PeopleDbContext()
    {
        BsonSerializer.RegisterSerializer(new AccountIdSerializer());
        BsonSerializer.RegisterSerializer(new CountryCodeSerializer());
        BsonSerializer.RegisterSerializer(new DateFormatSerializer());
        BsonSerializer.RegisterSerializer(new LanguageSerializer());
        BsonSerializer.RegisterSerializer(new TimeFormatSerializer());
        BsonSerializer.RegisterSerializer(new TimeZoneSerializer());

        BsonClassMap.RegisterClassMap<Entity>(map => map.UnmapProperty(x => x.DomainEvents));

        BsonClassMap.RegisterClassMap<Entity<AccountId>>(map =>
        {
            map.AutoMap();
            map.MapIdProperty(x => x.Id);
        });

        BsonClassMap.RegisterClassMap<Account>(map =>
        {
            map.AutoMap();
            map.MapField("_password");
            map.MapField("_registration");
            map.MapField("_roles")
                .SetElementName(nameof(Account.Roles));
            map.MapField("_connections")
                .SetElementName(nameof(Account.Connections));
        });

        BsonClassMap.RegisterClassMap<EmailConnection>();
        BsonClassMap.RegisterClassMap<GoogleConnection>();
        BsonClassMap.RegisterClassMap<MicrosoftConnection>();
    }

    public PeopleDbContext(IOptions<MongoDbOptions> settings)
        : base(settings.Value)
    {
    }

    public IMongoCollection<Sequence> Sequences =>
        Database.GetCollection<Sequence>("sequences");

    public IMongoCollection<Account> Accounts =>
        Database.GetCollection<Account>("accounts");

    public IMongoCollection<Confirmation> Confirmations =>
        Database.GetCollection<Confirmation>("confirmations");
    
    public override async Task OnModelCreatingAsync()
    {
        await CreateCollectionsAsync(
            Accounts.CollectionNamespace.CollectionName,
            Sequences.CollectionNamespace.CollectionName,
            Confirmations.CollectionNamespace.CollectionName
        );

        await CreateIndexesAsync(Accounts,
            new CreateIndexModel<Account>(
                Builders<Account>.IndexKeys.Combine(
                    Builders<Account>.IndexKeys.Ascending($"{nameof(Account.Connections)}._t"),
                    Builders<Account>.IndexKeys.Ascending($"{nameof(Account.Connections)}.{nameof(Connection.Value)}")
                ),
                new CreateIndexOptions
                {
                    Name = "Connections.Type_Connections.Value",
                    Unique = true
                }
            )
        );

        await CreateIndexesAsync(Sequences,
            new CreateIndexModel<Sequence>(
                Builders<Sequence>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Name = "Name", Unique = true }
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

        var sequenceCount = await Sequences.CountDocumentsAsync(FilterDefinition<Sequence>.Empty);
        if (sequenceCount == 0)
            await Sequences.InsertManyAsync(SequenceGenerator.InitValues());
    }
}
