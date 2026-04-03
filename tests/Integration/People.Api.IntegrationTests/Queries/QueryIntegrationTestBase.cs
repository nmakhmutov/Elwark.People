using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Queries;

/// <summary>
/// Shared PostgreSQL + <see cref="CommandTestFixture"/> for query-handler integration tests.
/// </summary>
[Collection(nameof(PostgresCollection))]
public abstract class QueryIntegrationTestBase : IAsyncLifetime
{
    protected QueryIntegrationTestBase(PostgreSqlFixture postgres) =>
        Commands = new CommandTestFixture(postgres);

    protected CommandTestFixture Commands { get; }

    public Task InitializeAsync() =>
        Commands.InitializeAsync();

    public Task DisposeAsync() =>
        Commands.DisposeAsync();
}
