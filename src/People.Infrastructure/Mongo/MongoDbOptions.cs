namespace People.Infrastructure.Mongo
{
    public sealed record MongoDbOptions
    {
        public string ConnectionString { get; init; } = string.Empty;
        
        public string Database { get; init; } = string.Empty;
    }
}
