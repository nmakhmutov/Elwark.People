using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using People.Infrastructure.Integration;
using People.Infrastructure.Kafka.Configurations;
using People.Infrastructure.Kafka.Converters;
using Polly;
using Polly.Retry;

namespace People.Infrastructure.Kafka;

internal sealed class KafkaConsumer<TEvent, THandler> : BackgroundService
    where TEvent : IIntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    private readonly AdminClientBuilder _adminBuilder;
    private readonly IHostApplicationLifetime _application;
    private readonly ConsumerBuilder<string, TEvent> _consumerBuilder;
    private readonly ILogger<KafkaConsumer<TEvent, THandler>> _logger;
    private readonly KafkaConsumerConfig<TEvent> _options;
    private readonly AsyncRetryPolicy _policy;
    private readonly IServiceScopeFactory _serviceFactory;

    public KafkaConsumer(IHostApplicationLifetime application, ILogger<KafkaConsumer<TEvent, THandler>> logger,
        IOptions<KafkaConsumerConfig<TEvent>> handlerOptions, IOptions<ConsumerConfig> consumerOptions,
        IServiceScopeFactory serviceFactory)
    {
        _logger = logger;
        _application = application;
        _serviceFactory = serviceFactory;
        _options = handlerOptions.Value;

        _adminBuilder = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = consumerOptions.Value.BootstrapServers
        });

        _consumerBuilder = new ConsumerBuilder<string, TEvent>(consumerOptions.Value)
            .SetValueDeserializer(KafkaDataConverter<TEvent>.Instance);

        _policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(_options.RetryCount, _ => _options.RetryInterval, (ex, time, retry, _) =>
                _logger.LogHandlerException(ex, _options.EventName, retry, time)
            );
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await CreateTopicIfNotExistsAsync()
            .ConfigureAwait(false);

        switch (_options.Threads)
        {
            case < 1:
                _logger.LogEmptyHandler(_options.EventName, _options.Topic);
                break;

            case 1:
                await Task.Factory
                    .StartNew(() => CreateConsumer(ct), TaskCreationOptions.LongRunning)
                    .ConfigureAwait(false);
                break;

            case > 1:
                var tasks = new Task[_options.Threads];
                for (var i = 0; i < _options.Threads; i++)
                    tasks[i] = Task.Factory.StartNew(() => CreateConsumer(ct), TaskCreationOptions.LongRunning);

                await Task
                    .WhenAll(tasks)
                    .ConfigureAwait(false);
                break;
        }
    }

    private async Task CreateConsumer(CancellationToken ct)
    {
        using var consumer = _consumerBuilder.Build();
        consumer.Subscribe(_options.Topic);
        var name = GetConsumerName(consumer);

        _logger.LogConsumerSubscribed(_options.EventName, _options.Topic, name);

        ct.Register(() => _logger.LogConsumerStopping(_options.EventName, _options.Topic, name));

        while (!ct.IsCancellationRequested)
            await ConsumeAsync(consumer, ct)
                .ConfigureAwait(false);
    }

    private async Task ConsumeAsync(IConsumer<string, TEvent> consumer, CancellationToken ct)
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
            _logger.LogConsumerException(ex, _options.EventName, _options.Topic, consumer.MemberId);
            _application.StopApplication();

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogUnhandledException(ex, _options.EventName);
        }
    }

    private async Task HandleAsync(Message<string, TEvent> message, string consumer)
    {
        await using var scope = _serviceFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<TEvent>>();

        _logger.LogMessageReceived(_options.EventName, _options.Topic, consumer, message.Key);
        _ = await _policy.ExecuteAndCaptureAsync(() => handler.HandleAsync(message.Value)).ConfigureAwait(false) switch
        {
            { Outcome: OutcomeType.Successful } =>
                _logger.LogMessageHandled(_options.EventName, _options.Topic, consumer, message.Key),

            { Outcome: OutcomeType.Failure } =>
                _logger.LogMessageUnhandled(_options.EventName, _options.Topic, consumer, message.Key),

            _ => false
        };
    }

    private async Task CreateTopicIfNotExistsAsync()
    {
        var topic = new TopicSpecification
        {
            Name = _options.Topic,
            NumPartitions = _options.Threads * 2,
            ReplicationFactor = 1,
            Configs = new Dictionary<string, string>
            {
                ["cleanup.policy"] = "delete",
                ["retention.ms"] = TimeSpan.FromDays(7).TotalMilliseconds.ToString("0000")
            }
        };

        using var client = _adminBuilder.Build();

        try
        {
            await client.CreateTopicsAsync(new[] { topic })
                .ConfigureAwait(false);

            _logger.LogInformation("Topic {T} created with {P} partitions", topic.Name, topic.NumPartitions);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // everything is fine!
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Exception occured while creating topic {T}", topic.Name);
            _application.StopApplication();

            throw;
        }
    }

    private static string GetConsumerName(IConsumer<string, TEvent> consumer) =>
        string.IsNullOrEmpty(consumer.MemberId) ? consumer.Name : consumer.MemberId;
}

internal static class Log
{
    private static readonly Action<ILogger, string, string, string, Exception?> ConsumerSubscribed =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(1, nameof(ConsumerSubscribed)),
            "Consumer {C} for message {M} from topic {T} has been subscribed"
        );

    private static readonly Action<ILogger, string, string, string, Exception?> ConsumerStopping =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(2, nameof(ConsumerStopping)),
            "Consumer {C} for message {M} from topic {T} stopping..."
        );

    private static readonly Action<ILogger, string, string, string, string, Exception?> MessageReceived =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(3, nameof(MessageReceived)),
            "Message {M} with key {K} from topic {T} is processed by the consumer {C}..."
        );

    private static readonly Action<ILogger, string, string, string, string, Exception?> MessageHandled =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(4, nameof(MessageHandled)),
            "Message {M} with key {K} from topic {T} has been processed by the consumer {C}"
        );

    private static readonly Action<ILogger, string, string, string, string, Exception?> MessageUnhandled =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Error,
            new EventId(5, nameof(MessageUnhandled)),
            "Message {M} with key {K} from topic {T} has not been processed by the consumer {C}"
        );

    private static readonly Action<ILogger, string, string, Exception?> EmptyHandler =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(6, nameof(EmptyHandler)),
            "Topic {T} doesn't have any consumer handlers for messages {M}"
        );

    private static readonly Action<ILogger, string, string, string, Exception?> ConsumerException =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(7, nameof(ConsumerException)),
            "Consumer {C} threw a fatal exception while processing messages {M} from topic {T}"
        );

    private static readonly Action<ILogger, string, int, TimeSpan, Exception?> HandlerException =
        LoggerMessage.Define<string, int, TimeSpan>(
            LogLevel.Error,
            new EventId(8, nameof(HandlerException)),
            "Error occured in kafka handler for message {M}. Retry {R} ({T})"
        );

    private static readonly Action<ILogger, string, Exception?> UnhandledException =
        LoggerMessage.Define<string>(
            LogLevel.Critical,
            new EventId(9, nameof(HandlerException)),
            "Unhandled exception occured in kafka consumer for message {M}"
        );

    public static void LogConsumerSubscribed(this ILogger logger, string evt, string topic, string consumer) =>
        ConsumerSubscribed(logger, consumer, evt, topic, null);

    public static void LogConsumerStopping(this ILogger logger, string evt, string topic, string consumer) =>
        ConsumerStopping(logger, consumer, evt, topic, null);

    public static void LogMessageReceived(this ILogger logger, string evt, string topic, string consumer, string key) =>
        MessageReceived(logger, evt, key, topic, consumer, null);

    public static bool LogMessageHandled(this ILogger logger, string evt, string topic, string consumer, string key)
    {
        MessageHandled(logger, evt, key, topic, consumer, null);
        return true;
    }

    public static bool LogMessageUnhandled(this ILogger logger, string evt, string topic, string consumer, string key)
    {
        MessageUnhandled(logger, evt, key, topic, consumer, null);
        return true;
    }

    public static void LogEmptyHandler(this ILogger logger, string evt, string topic) =>
        EmptyHandler(logger, evt, topic, null);

    public static void LogConsumerException(this ILogger logger, ConsumeException ex, string evt, string topic,
        string consumer) =>
        ConsumerException(logger, consumer, evt, topic, ex);

    public static void LogHandlerException(this ILogger logger, Exception ex, string evt, int retry, TimeSpan delay) =>
        HandlerException(logger, evt, retry, delay, ex);

    public static void LogUnhandledException(this ILogger logger, Exception ex, string evt) =>
        UnhandledException(logger, evt, ex);
}
