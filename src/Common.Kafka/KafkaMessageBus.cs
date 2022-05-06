using System.Collections.Concurrent;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Common.Kafka;

internal sealed class KafkaMessageBus : IIntegrationEventBus
{
    private static readonly ConcurrentDictionary<Type, Type> Types = new();
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<KafkaMessageBus> _logger;
    private readonly AsyncRetryPolicy _policy;

    public KafkaMessageBus(IServiceScopeFactory factory, ILogger<KafkaMessageBus> logger)
    {
        var sleepDurations = new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30)
        };

        _factory = factory;
        _logger = logger;
        _policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(sleepDurations, (ex, t, i, _) =>
                _logger.LogCritical(ex, "Sending event failed: {m}. Attempt {i}. Time: {t}", ex.Message, i, t)
            );
    }

    public async Task PublishAsync<T>(T message, CancellationToken ct) where T : IIntegrationEvent
    {
        var type = message.GetType();
        var handlerType = Types.GetOrAdd(type, static x => typeof(IKafkaProducer<>).MakeGenericType(x));

        await using var scope = _factory.CreateAsyncScope();
        var producer = scope.ServiceProvider.GetRequiredService(handlerType) as IKafkaProducer
                       ?? throw new KafkaException(ErrorCode.InvalidMsg, new Exception($"{type}'s producer not found"));

        await _policy.ExecuteAsync(token => producer.ProduceAsync(message, token), ct)
            .ConfigureAwait(false);

        _logger.LogInformation("Kafka message {m} sent.", message);
    }

    public Task PublishAsync<T>(IEnumerable<T> messages, CancellationToken ct) where T : IIntegrationEvent =>
        Parallel.ForEachAsync(messages, ct, async (message, token) =>
            await PublishAsync(message, token)
                .ConfigureAwait(false)
        );
}
