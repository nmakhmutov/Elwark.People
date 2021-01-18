using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Prohibitions;

namespace People.Infrastructure
{
    public sealed class InfrastructureDbContext : MongoDbContext
    {
        public InfrastructureDbContext(IOptions<DbContextSettings> settings)
            : base(settings.Value)
        {
        }

        public IMongoCollection<Prohibition> Prohibitions =>
            Database.GetCollection<Prohibition>("prohibitions");

        public IMongoCollection<Confirmation> Confirmations =>
            Database.GetCollection<Confirmation>("confirmations");

        public override async Task OnModelCreatingAsync()
        {
            await CreateCollectionsAsync(
                Prohibitions.CollectionNamespace.CollectionName,
                Confirmations.CollectionNamespace.CollectionName
            );

            await CreateIndexesAsync(Prohibitions,
                new CreateIndexModel<Prohibition>(
                    Builders<Prohibition>.IndexKeys.Combine(
                        Builders<Prohibition>.IndexKeys.Ascending(x => x.Type),
                        Builders<Prohibition>.IndexKeys.Ascending(x => x.Value)
                    ),
                    new CreateIndexOptions {Name = "Type_Value"}
                )
            );

            await CreateIndexesAsync(Confirmations,
                new CreateIndexModel<Confirmation>(
                    Builders<Confirmation>.IndexKeys.Descending(x => x.ExpireAt),
                    new CreateIndexOptions {Name = "ExpireAt", ExpireAfter = TimeSpan.Zero}
                ),
                new CreateIndexModel<Confirmation>(
                    Builders<Confirmation>.IndexKeys.Combine(
                        Builders<Confirmation>.IndexKeys.Ascending(x => x.AccountId),
                        Builders<Confirmation>.IndexKeys.Ascending(x => x.Type),
                        Builders<Confirmation>.IndexKeys.Descending(x => x.ExpireAt)
                    ),
                    new CreateIndexOptions {Name = "AccountId_Type_ExpireAt"}
                )
            );
        }
    }
}