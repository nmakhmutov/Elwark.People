using MongoDB.Driver;

namespace Common.Mongo;

public abstract class MongoDbContext : IDisposable
{
    private readonly MongoClient _client;

    protected MongoDbContext(MongoDbOptions settings)
    {
        var mongoSettings = MongoClientSettings.FromUrl(new MongoUrl(settings.ConnectionString));

        // mongoSettings.ClusterConfigurator = builder =>
        //     builder.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(e =>
        //         System.Console.WriteLine(MongoDB.Bson.BsonExtensionMethods.ToJson(e.Command,
        //             new MongoDB.Bson.IO.JsonWriterSettings {Indent = true})));

        _client = new MongoClient(mongoSettings);
        Database = _client.GetDatabase(settings.Database);
    }

    protected IMongoDatabase Database { get; }

    public IClientSessionHandle? Session { get; private set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask StartSessionAsync(CancellationToken ct = default)
    {
        if (Session is not null)
            return;

        Session = await _client.StartSessionAsync(new ClientSessionOptions(), ct);
        Session.StartTransaction();
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (Session is null)
            return;

        await Session.CommitTransactionAsync(ct);

        Session.Dispose();
        Session = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (Session is null)
            return;

        await Session.AbortTransactionAsync(ct);

        Session.Dispose();
        Session = null;
    }

    protected async Task CreateCollectionsAsync(params string[] collectionNames)
    {
        var collections = await (await Database.ListCollectionNamesAsync()).ToListAsync();

        foreach (var name in collectionNames)
            if (!collections.Contains(name))
                await Database.CreateCollectionAsync(name);
    }

    protected async Task CreateIndexesAsync<T>(IMongoCollection<T> collection,
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

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        Session?.Dispose();
        Session = null;
    }
}
