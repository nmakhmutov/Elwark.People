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
    private static readonly ConcurrentDictionary<Type, Type> Types = [];
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<KafkaEventBus> _logger;
    private readonly ResiliencePipeline _policy;

    public KafkaEventBus(IServiceScopeFactory factory, ILogger<KafkaEventBus> logger)
    {
        _factory = factory;
        _logger = logger;
        _policy = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 6,
                DelayGenerator = x =>
                {
                    TimeSpan? delay = x.AttemptNumber switch
                    {
                        0 => TimeSpan.Zero,
                        1 => TimeSpan.FromSeconds(1),
                        2 => TimeSpan.FromSeconds(5),
                        3 => TimeSpan.FromSeconds(10),
                        4 => TimeSpan.FromSeconds(20),
                        5 => TimeSpan.FromSeconds(30),
                        _ => null
                    };

                    return ValueTask.FromResult(delay);
                },
                OnRetry = x =>
                {
                    _logger.PublisherException(x.Outcome.Exception, x.AttemptNumber, x.RetryDelay);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task PublishAsync<T>(T message, CancellationToken ct) where T : IIntegrationEvent
    {
        await using var scope = _factory.CreateAsyncScope();

        await PublishAsync(scope.ServiceProvider, message, ct);
    }

    public async Task PublishAsync<T>(ICollection<T> messages, CancellationToken ct) where T : IIntegrationEvent
    {
        if (messages.Count == 0)
            return;

        await using var scope = _factory.CreateAsyncScope();

        await Parallel.ForEachAsync(messages, ct, async (message, token) =>
        {
            token.ThrowIfCancellationRequested();

            await PublishAsync(scope.ServiceProvider, message, token);
        });
    }

    private async Task PublishAsync<T>(IServiceProvider provider, T message, CancellationToken ct)
        where T : IIntegrationEvent
    {
        var type = message.GetType();
        var handlerType = Types.GetOrAdd(type, x =>
            GetProducerType(provider, x) ?? throw CreateException("Unknown producer type")
        );

        var producer = provider.GetRequiredService(handlerType) as IKafkaProducer
                       ?? throw CreateException($"{type.Name}'s producer not found");

        _logger.MessageSending(message);

        await _policy.ExecuteAsync(async token => await producer.ProduceAsync(message, token), ct);

        _logger.MessageSent(message);
    }

    private static Type? GetProducerType(IServiceProvider provider, Type type)
    {
        if (type == typeof(object))
            return null;

        var handler = typeof(IKafkaProducer<>).MakeGenericType(type);
        if (provider.GetService(handler) is IKafkaProducer)
            return handler;

        return type.BaseType is null ? null : GetProducerType(provider, type.BaseType);
    }

    private static KafkaException CreateException(string message) =>
        new(ErrorCode.InvalidMsg, new Exception(message));
}
