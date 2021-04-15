using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.AggregateModels.EmailProvider;
using People.Domain.SeedWork;
using People.Infrastructure.Mongo;
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
                map.MapField("_registration");
                map.MapField("_lastSignIn");
                map.MapField("_roles")
                    .SetElementName(nameof(Account.Roles));
                map.MapField("_identities")
                    .SetElementName(nameof(Account.Identities));
            });

            BsonClassMap.RegisterClassMap<EmailIdentityModel>();
            BsonClassMap.RegisterClassMap<GoogleIdentityModel>();
            BsonClassMap.RegisterClassMap<MicrosoftIdentityModel>();
            
            BsonClassMap.RegisterClassMap<Entity<EmailProviderType>>(map =>
            {
                map.AutoMap();
                map.MapIdProperty(x => x.Id);
            });

            BsonClassMap.RegisterClassMap<Sendgrid>();
            BsonClassMap.RegisterClassMap<Gmail>();
        }

        public PeopleDbContext(IOptions<DbContextSettings> settings)
            : base(settings.Value)
        {
        }

        public IMongoCollection<Sequence> Sequences =>
            Database.GetCollection<Sequence>("sequences");

        public IMongoCollection<Account> Accounts =>
            Database.GetCollection<Account>("accounts");

        public IMongoCollection<EmailProvider> EmailProviders =>
            Database.GetCollection<EmailProvider>("email_providers");
        
        public override async Task OnModelCreatingAsync()
        {
            await CreateCollectionsAsync(
                Accounts.CollectionNamespace.CollectionName,
                Sequences.CollectionNamespace.CollectionName,
                EmailProviders.CollectionNamespace.CollectionName
            );
            
            await CreateIndexesAsync(Accounts,
                new CreateIndexModel<Account>(
                    Builders<Account>.IndexKeys.Combine(
                        Builders<Account>.IndexKeys.Ascending(
                            $"{nameof(Account.Identities)}.{nameof(IdentityModel.Type)}"
                        ),
                        Builders<Account>.IndexKeys.Ascending(
                            $"{nameof(Account.Identities)}.{nameof(IdentityModel.Value)}"
                        )
                    ),
                    new CreateIndexOptions
                    {
                        Name = "Identities.Type_Identities.Value",
                        Unique = true
                    }
                )
            );

            await CreateIndexesAsync(Sequences,
                new CreateIndexModel<Sequence>(
                    Builders<Sequence>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions {Name = "Name", Unique = true}
                )
            );

            await CreateIndexesAsync(EmailProviders,
                new CreateIndexModel<EmailProvider>(
                    Builders<EmailProvider>.IndexKeys.Descending(x => x.Balance),
                    new CreateIndexOptions {Name = "Balance"}
                )
            );
            
            var sequenceCount = await Sequences.CountDocumentsAsync(FilterDefinition<Sequence>.Empty);
            if (sequenceCount == 0)
                await Sequences.InsertManyAsync(SequenceGenerator.InitValues());
        }
    }
}
