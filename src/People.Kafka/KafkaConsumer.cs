using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using People.Kafka.Configurations;
using People.Kafka.Converters;
using People.Kafka.Integration;
using Polly;
using Polly.Retry;

namespace People.Kafka;

internal sealed class KafkaConsumer<TEvent, THandler> : BackgroundService
    where TEvent : IIntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    private readonly ConsumerConfiguration _configuration;
    private readonly ILogger<KafkaConsumer<TEvent, THandler>> _logger;
    private readonly AsyncRetryPolicy _policy;
    private readonly IServiceScopeFactory _serviceFactory;

    public KafkaConsumer(ConsumerConfiguration configuration, IServiceScopeFactory serviceFactory,
        ILogger<KafkaConsumer<TEvent, THandler>> logger)
    {
        _configuration = configuration;
        _serviceFactory = serviceFactory;
        _logger = logger;

        _policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(configuration.RetryCount, _ => configuration.RetryInterval, (ex, time, retry, _) =>
                _logger.LogHandlerException(ex, retry, time)
            );
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (_configuration.TopicSpecification is not null)
            await CreateTopicIfNotExistsAsync(_configuration.TopicSpecification)
                .ConfigureAwait(false);

        var tasks = new Task[_configuration.Workers];
        for (var i = 0; i < _configuration.Workers; i++)
            tasks[i] = Task.Factory.StartNew(() => CreateConsumer(ct), TaskCreationOptions.LongRunning);

        await Task.WhenAll(tasks)
            .ConfigureAwait(false);
    }

    private async Task CreateConsumer(CancellationToken ct)
    {
        using var consumer = new ConsumerBuilder<Guid, TEvent>(_configuration.Config)
            .SetKeyDeserializer(KafkaKeyConverter.Instance)
            .SetValueDeserializer(KafkaValueConverter<TEvent>.Instance)
            .Build();

        consumer.Subscribe(_configuration.Topic);
        while (string.IsNullOrEmpty(consumer.MemberId))
            await Task.Delay(500, ct);

        _logger.LogConsumerSubscribed(_configuration.Topic, consumer.MemberId);

        while (!ct.IsCancellationRequested)
            await ConsumeAsync(consumer, ct)
                .ConfigureAwait(false);
    }

    private async Task ConsumeAsync(IConsumer<Guid, TEvent> consumer, CancellationToken ct)
    {
        try
        {
            var result = consumer.Consume(ct);
            if (result.IsPartitionEOF)
                return;

            await HandleAsync(result.Message, consumer.MemberId)
                .ConfigureAwait(false);

            consumer.Commit(result);
        }
        catch (ConsumeException ex) when (ex.Error.IsFatal)
        {
            _logger.LogConsumerException(ex, _configuration.Topic, consumer.MemberId);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogConsumerCanceled(_configuration.Topic, consumer.MemberId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogUnhandledException(ex, _configuration.Topic);
        }
    }

    private async Task HandleAsync(Message<Guid, TEvent> message, string consumer)
    {
        await using var scope = _serviceFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<TEvent>>();

        _logger.LogMessageReceived(_configuration.Topic, consumer, message.Key);

        var result = await _policy.ExecuteAndCaptureAsync(() => handler.HandleAsync(message.Value))
            .ConfigureAwait(false);

        if (result.Outcome == OutcomeType.Successful)
            _logger.LogMessageHandled(_configuration.Topic, consumer, message.Key);
        else
            _logger.LogMessageUnhandled(_configuration.Topic, consumer, message.Key);
    }

    private async Task CreateTopicIfNotExistsAsync(TopicSpecification specification)
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = _configuration.Config.BootstrapServers
        };

        using var client = new AdminClientBuilder(config)
            .Build();

        try
        {
            await client.CreateTopicsAsync(new[] { specification })
                .ConfigureAwait(false);

            _logger.LogInformation("Topic {T} created {D}", specification.Name, specification);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // everything is fine!
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Exception occured while creating topic {T}", specification.Name);

            throw;
        }
    }
}

internal static class Log
{
    private static readonly Action<ILogger, string, string, Exception?> ConsumerSubscribed =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(1, nameof(ConsumerSubscribed)),
            "Consumer {C} for topic {T} has been subscribed"
        );

    private static readonly Action<ILogger, Guid, string, string, Exception?> MessageReceived =
        LoggerMessage.Define<Guid, string, string>(
            LogLevel.Information,
            new EventId(2, nameof(MessageReceived)),
            "Message with key {K} from topic {T} is processed by the consumer {C}..."
        );

    private static readonly Action<ILogger, Guid, string, string, Exception?> MessageHandled =
        LoggerMessage.Define<Guid, string, string>(
            LogLevel.Information,
            new EventId(3, nameof(MessageHandled)),
            "Message with key {K} from topic {T} has been processed by the consumer {C}"
        );

    private static readonly Action<ILogger, Guid, string, string, Exception?> MessageUnhandled =
        LoggerMessage.Define<Guid, string, string>(
            LogLevel.Error,
            new EventId(4, nameof(MessageUnhandled)),
            "Message with key {K} from topic {T} has not been processed by the consumer {C}"
        );

    private static readonly Action<ILogger, string, string, Exception?> ConsumerException =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(5, nameof(ConsumerException)),
            "Consumer {C} threw a fatal exception while processing messages from topic {T}"
        );

    private static readonly Action<ILogger, int, TimeSpan, Exception?> HandlerException =
        LoggerMessage.Define<int, TimeSpan>(
            LogLevel.Error,
            new EventId(6, nameof(HandlerException)),
            "Error occured in kafka handler. Retry {R} ({T})"
        );

    private static readonly Action<ILogger, string, Exception?> UnhandledException =
        LoggerMessage.Define<string>(
            LogLevel.Critical,
            new EventId(7, nameof(HandlerException)),
            "Unhandled exception occured in kafka consumer in topic {T}"
        );

    private static readonly Action<ILogger, string, string, Exception?> ConsumerCanceled =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(8, nameof(ConsumerCanceled)),
            "Consumer {C} for topic {T} has been canceled"
        );

    public static void LogConsumerSubscribed(this ILogger logger, string topic, string consumer) =>
        ConsumerSubscribed(logger, consumer, topic, null);

    public static void LogMessageReceived(this ILogger logger, string topic, string consumer, Guid key) =>
        MessageReceived(logger, key, topic, consumer, null);

    public static void LogMessageHandled(this ILogger logger, string topic, string consumer, Guid key) =>
        MessageHandled(logger, key, topic, consumer, null);

    public static void LogMessageUnhandled(this ILogger logger, string topic, string consumer, Guid key) =>
        MessageUnhandled(logger, key, topic, consumer, null);

    public static void LogConsumerException(this ILogger logger, ConsumeException ex, string topic, string consumer) =>
        ConsumerException(logger, consumer, topic, ex);

    public static void LogHandlerException(this ILogger logger, Exception ex, int retry, TimeSpan delay) =>
        HandlerException(logger, retry, delay, ex);

    public static void LogUnhandledException(this ILogger logger, Exception ex, string topic) =>
        UnhandledException(logger, topic, ex);

    public static void LogConsumerCanceled(this ILogger logger, string topic, string consumer) =>
        ConsumerCanceled(logger, consumer, topic, null);
}
