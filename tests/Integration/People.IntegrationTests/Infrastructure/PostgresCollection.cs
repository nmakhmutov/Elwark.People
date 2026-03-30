using Xunit;

namespace People.IntegrationTests.Infrastructure;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgreSqlFixture>;
