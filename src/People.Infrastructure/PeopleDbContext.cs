using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.SeedWork;
using People.Infrastructure.Sequences;
using People.Infrastructure.Serializers;

namespace People.Infrastructure
{
    public sealed class PeopleDbContext : MongoDbContext
    {
        static PeopleDbContext()
        {
            BsonSerializer.RegisterSerializer(new AccountIdSerializer());
            BsonSerializer.RegisterSerializer(new CountryCodeSerializer());
            BsonSerializer.RegisterSerializer(new LanguageSerializer());

            BsonClassMap.RegisterClassMap<Entity>(map =>
            {
                map.UnmapField("_domainEvents");
                map.UnmapProperty(f => f.DomainEvents);
            });

            BsonClassMap.RegisterClassMap<Entity<AccountId>>(map =>
            {
                map.AutoMap();
                map.MapIdProperty(x => x.Id);
            });

            BsonClassMap.RegisterClassMap<Account>(map =>
            {
                map.AutoMap();
                map.MapField("_password");
                map.MapField("_roles")
                    .SetElementName(nameof(Account.Roles));
                map.MapField("_identities")
                    .SetElementName(nameof(Account.Identities));
            });

            BsonClassMap.RegisterClassMap<EmailIdentity>();
            BsonClassMap.RegisterClassMap<GoogleIdentity>();
            BsonClassMap.RegisterClassMap<FacebookIdentity>();
            BsonClassMap.RegisterClassMap<MicrosoftIdentity>();
        }

        public PeopleDbContext(IOptions<DbContextSettings> settings)
            : base(settings.Value)
        {
        }

        public IMongoCollection<Sequence> Sequences =>
            Database.GetCollection<Sequence>("sequences");

        public IMongoCollection<Account> Accounts =>
            Database.GetCollection<Account>("accounts");

        public override async Task OnModelCreatingAsync()
        {
            await CreateCollectionsAsync(
                Accounts.CollectionNamespace.CollectionName,
                Sequences.CollectionNamespace.CollectionName
            );

            await CreateIndexesAsync(Accounts,
                new CreateIndexModel<Account>(
                    Builders<Account>.IndexKeys.Combine(
                        Builders<Account>.IndexKeys.Ascending(
                            $"{nameof(Account.Identities)}.{nameof(Identity.Key)}.{nameof(IdentityKey.Type)}"
                        ),
                        Builders<Account>.IndexKeys.Ascending(
                            $"{nameof(Account.Identities)}.{nameof(Identity.Key)}.{nameof(IdentityKey.Value)}"
                        )
                    ),
                    new CreateIndexOptions {Name = "Identities.Key.Type_Identities.Key.Value", Unique = true}
                )
            );

            await CreateIndexesAsync(Sequences,
                new CreateIndexModel<Sequence>(
                    Builders<Sequence>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions {Name = "Name", Unique = true}
                )
            );

            var sequenceCount = await Sequences.CountDocumentsAsync(FilterDefinition<Sequence>.Empty);
            if (sequenceCount == 0)
                await Sequences.InsertManyAsync(SequenceGenerator.InitValues());
        }
    }
}