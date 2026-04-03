using Integration.Shared.Tests.Infrastructure;
using Xunit;

namespace Integration.Api.Tests.Infrastructure;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgreSqlFixture>;
