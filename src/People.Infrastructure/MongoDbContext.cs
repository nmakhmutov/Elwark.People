using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace People.Infrastructure
{
    public abstract class MongoDbContext
    {
        protected readonly IMongoDatabase Database;

        protected MongoDbContext(DbContextSettings settings)
        {
            var mongoSettings = MongoClientSettings.FromUrl(new MongoUrl(settings.ConnectionString));
            // mongoSettings.ClusterConfigurator =
            //     builder => builder.Subscribe<CommandStartedEvent>(e =>
            //         Console.WriteLine(e.Command.ToJson(new JsonWriterSettings {Indent = true})));
            var client = new MongoClient(mongoSettings);

            if (client is null)
                throw new MongoException("Client is null");

            Database = client.GetDatabase(settings.Database);
        }

        protected async Task CreateCollectionsAsync(params string[] collectionNames)
        {
            var collections = await (await Database.ListCollectionNamesAsync()).ToListAsync();

            foreach (var name in collectionNames)
                if (!collections.Contains(name))
                    await Database.CreateCollectionAsync(name);
        }

        protected static async Task CreateIndexesAsync<T>(IMongoCollection<T> collection,
            params CreateIndexModel<T>[] indexes)
        {
            var dbIndexNames = new List<string>();
            await (await collection.Indexes.ListAsync())
                .ForEachAsync(document => dbIndexNames.Add(document["name"].AsString));

            var newIndexes = indexes.Where(x => !dbIndexNames.Contains(x.Options.Name))
                .ToArray();

            if (newIndexes.Length > 0)
                await collection.Indexes.CreateManyAsync(newIndexes);
        }

        public abstract Task OnModelCreatingAsync();
    }
}