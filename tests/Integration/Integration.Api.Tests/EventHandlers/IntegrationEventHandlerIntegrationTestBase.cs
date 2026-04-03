using Xunit;

namespace Integration.Api.Tests.EventHandlers;

[Collection(nameof(PostgresCollection))]
public abstract class IntegrationEventHandlerIntegrationTestBase(PostgreSqlFixture postgres) : IAsyncLifetime
{
    protected IntegrationEventHandlerTestFixture Fx { get; } = new(postgres);

    public Task InitializeAsync() =>
        Fx.InitializeAsync();

    public Task DisposeAsync() =>
        Fx.DisposeAsync();
}
