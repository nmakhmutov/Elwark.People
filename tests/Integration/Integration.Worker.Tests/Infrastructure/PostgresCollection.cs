using Integration.Shared.Tests.Infrastructure;
using Xunit;

namespace Integration.Worker.Tests.Infrastructure;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgreSqlFixture>;
