namespace People.Infrastructure.Mongo
{
    public sealed record DbContextSettings
    {
        public string ConnectionString { get; init; } = string.Empty;
        
        public string Database { get; init; } = string.Empty;
    }
}