using People.Kafka.Integration;

namespace People.IntegrationTests.Infrastructure;

internal static class TestDbContextFactory
{
    internal static IIntegrationEventBus CreateNoOpIntegrationEventBus() =>
        new NoOpIntegrationEventBus();

    private sealed class NoOpIntegrationEventBus : IIntegrationEventBus
    {
        public ValueTask PublishAsync<T>(T message, CancellationToken ct = default)
            where T : IIntegrationEvent =>
            ValueTask.CompletedTask;

        public ValueTask PublishAsync<T>(ICollection<T> messages, CancellationToken ct = default)
            where T : IIntegrationEvent =>
            ValueTask.CompletedTask;
    }
}
