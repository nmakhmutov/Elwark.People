using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Common.Kafka;

internal sealed class KafkaConsumer<Event, Handler> : BackgroundService
    where Event : IIntegrationEvent
    where Handler : class, IKafkaHandler<Event>
{
    private readonly IHostApplicationLifetime _application;
    private readonly ConsumerConfig _consumerConfig;
    private readonly KafkaConsumerConfig<Event> _handlerOptions;
    private readonly ILogger<KafkaConsumer<Event, Handler>> _logger;
    private readonly AsyncRetryPolicy _policy;
    private readonly IServiceScopeFactory _serviceFactory;

    public KafkaConsumer(IServiceScopeFactory serviceFactory, IHostApplicationLifetime application,
        ILogger<KafkaConsumer<Event, Handler>> logger, IOptions<ConsumerConfig> consumerOptions,
        IOptions<KafkaConsumerConfig<Event>> handlerOptions)
    {
        _logger = logger;
        _application = application;
        _serviceFactory = serviceFactory;
        _consumerConfig = consumerOptions.Value;
        _handlerOptions = handlerOptions.Value;

        _policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(_handlerOptions.RetryCount, _ => _handlerOptions.RetryInterval, (ex, time, retry, _) =>
            {
                var level = retry > _handlerOptions.RetryCount * 0.5
                    ? LogLevel.Critical
                    : LogLevel.Warning;

                _logger.Log(level, ex, "Error occured in kafka handler for {N}. Retry {R}. Time {T}",
                    _handlerOptions.MessageType.Name, retry, time);
            });
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Consumer for {N} starting...", _handlerOptions.MessageType.Name);
        ct.Register(() =>
            _logger.LogInformation("Consumer for {N} shutting down...", _handlerOptions.MessageType.Name));

        await CreateTopicIfNotExistsAsync();

        var consumers = Enumerable.Range(0, _handlerOptions.Threads)
            .Select(_ => Task.Run(() => CreateConsumer(ct).ConfigureAwait(false), ct))
            .ToArray();

        await Task.WhenAll(consumers);
    }

    private async Task CreateConsumer(CancellationToken ct)
    {
        var builder = new ConsumerBuilder<string, Event>(_consumerConfig)
            .SetValueDeserializer(KafkaDataConverter<Event>.Instance);

        using var consumer = builder.Build();
        consumer.Subscribe(_handlerOptions.Topic);

        _logger.LogInformation("Consumer for {N} handling by {C} from topic {T}",
            _handlerOptions.MessageType.Name,
            consumer.Name,
            _handlerOptions.Topic
        );

        while (!ct.IsCancellationRequested)
            try
            {
                var result = consumer.Consume(ct);
                if (result.IsPartitionEOF)
                    continue;

                _logger.LogInformation("Consumer {N} received event {E} from topic {T}. {M}", consumer.Name,
                    _handlerOptions.MessageType.Name, _handlerOptions.Topic, result.Message.Value);

                await using var scope = _serviceFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<Event>>();

                await _policy.ExecuteAsync(() => handler.HandleAsync(result.Message.Value))
                    .ConfigureAwait(false);

                _logger.LogInformation("Consumer {N} handled event {E} from topic {T}", consumer.Name,
                    _handlerOptions.MessageType.Name, _handlerOptions.Topic);

                consumer.Commit(result);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Consumer {N} for message {M} canceled", consumer.Name,
                    _handlerOptions.MessageType.Name);

                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogWarning(ex, "Consumer exception in {N} for message {M}", consumer.Name,
                    _handlerOptions.MessageType.Name);

                if (!ex.Error.IsFatal)
                    continue;

                _logger.LogCritical(ex, "Consumer exception is fatal. Application will stop");
                _application.StopApplication();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled exception has occured in kafka message consumer");
            }

        consumer.Close();
        consumer.Dispose();
    }

    private async Task CreateTopicIfNotExistsAsync()
    {
        var config = new AdminClientConfig { BootstrapServers = _consumerConfig.BootstrapServers };
        var builder = new AdminClientBuilder(config);
        var topic = new TopicSpecification
        {
            Name = _handlerOptions.Topic,
            NumPartitions = _handlerOptions.Threads,
            ReplicationFactor = -1
        };

        using var client = builder.Build();

        try
        {
            await client.CreateTopicsAsync(new[] { topic });

            _logger.LogInformation("Topic {T} created with {P} partitions", topic.Name, topic.NumPartitions);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // everything is fine!
        }
        catch
        {
            _application.StopApplication();
        }
    }
}
