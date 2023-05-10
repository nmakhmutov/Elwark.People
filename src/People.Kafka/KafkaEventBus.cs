using System.Collections.Concurrent;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using People.Kafka.Integration;
using People.Kafka.Producers;
using Polly;
using Polly.Retry;

namespace People.Kafka;

internal sealed class KafkaEventBus : IIntegrationEventBus
{
    private static readonly ConcurrentDictionary<Type, Type> Types = new();
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<KafkaEventBus> _logger;
    private readonly AsyncRetryPolicy _policy;

    public KafkaEventBus(IServiceScopeFactory factory, ILogger<KafkaEventBus> logger)
    {
        var sleepDurations = new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(30)
        };

        _factory = factory;
        _logger = logger;
        _policy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(sleepDurations, (ex, t, i, _) => Log.LogHandlerException(_logger, ex, i, t));
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

        Log.LogMessageSent(_logger, type.Name, message.MessageId, message);
    }

    public Task PublishAsync<T>(IEnumerable<T> messages, CancellationToken ct) where T : IIntegrationEvent =>
        Parallel.ForEachAsync(messages, ct, async (message, token) =>
            await PublishAsync(message, token)
                .ConfigureAwait(false)
        );

    private static class Log
    {
        private static readonly Action<ILogger, string, Guid, object, Exception?> MessageSent =
            LoggerMessage.Define<string, Guid, object>(
                LogLevel.Debug,
                new EventId(1, nameof(MessageSent)),
                "Message {M} with key {K} has been sent. Message body is: {B}"
            );

        private static readonly Action<ILogger, string, int, TimeSpan, Exception?> HandlerException =
            LoggerMessage.Define<string, int, TimeSpan>(
                LogLevel.Critical,
                new EventId(2, nameof(HandlerException)),
                "Sending event failed: {M}. Retry {R} ({T})"
            );

        public static void LogMessageSent(ILogger logger, string evt, Guid key, object body) =>
            MessageSent(logger, evt, key, body, null);

        public static void LogHandlerException(ILogger logger, Exception ex, int retry, TimeSpan delay) =>
            HandlerException(logger, ex.Message, retry, delay, ex);
    }
}
