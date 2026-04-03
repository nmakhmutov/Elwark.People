using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Commands;

/// <summary>
/// Base for command integration tests: shared PostgreSQL collection + <see cref="CommandTestFixture"/> lifecycle.
/// </summary>
[Collection(nameof(PostgresCollection))]
public abstract class CommandIntegrationTestBase : IAsyncLifetime
{
    protected CommandIntegrationTestBase(PostgreSqlFixture postgres) =>
        Commands = new CommandTestFixture(postgres);

    protected CommandTestFixture Commands { get; }

    public Task InitializeAsync() =>
        Commands.InitializeAsync();

    public Task DisposeAsync() =>
        Commands.DisposeAsync();
}
