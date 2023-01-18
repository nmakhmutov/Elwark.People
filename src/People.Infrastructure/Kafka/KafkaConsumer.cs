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
    private readonly IHostApplicationLifetime _application;
    private readonly ConsumerConfig _consumerConfig;
    private readonly KafkaConsumerConfig<TEvent> _handlerOptions;
    private readonly ILogger<KafkaConsumer<TEvent, THandler>> _logger;
    private readonly AsyncRetryPolicy _policy;
    private readonly IServiceScopeFactory _serviceFactory;

    public KafkaConsumer(IServiceScopeFactory serviceFactory, IHostApplicationLifetime application,
        ILogger<KafkaConsumer<TEvent, THandler>> logger, IOptions<ConsumerConfig> consumerOptions,
        IOptions<KafkaConsumerConfig<TEvent>> handlerOptions)
    {
        _logger = logger;
        _application = application;
        _serviceFactory = serviceFactory;
        _consumerConfig = consumerOptions.Value;
        _handlerOptions = handlerOptions.Value;

        _policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _handlerOptions.RetryCount,
                _ => _handlerOptions.RetryInterval,
                (ex, time, retry, _) =>
                    _logger.LogCritical(ex, "Error occured in kafka handler for {N}. Retry {R} ({T})",
                        _handlerOptions.EventName, retry, time)
            );
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await CreateTopicIfNotExistsAsync();

        switch (_handlerOptions.Threads)
        {
            case < 1:
                _logger.LogWarning("Topic {T} doesn't have any consumer handlers for messages {M}",
                    _handlerOptions.Topic, _handlerOptions.EventName);
                break;

            case 1:
                await Task.Factory.StartNew(() => CreateConsumer(ct), TaskCreationOptions.LongRunning);
                break;

            case > 1:
                var tasks = new Task[_handlerOptions.Threads];
                for (var i = 0; i < _handlerOptions.Threads; i++)
                    tasks[i] = Task.Factory.StartNew(() => CreateConsumer(ct), TaskCreationOptions.LongRunning);

                await Task.WhenAll(tasks);
                break;
        }
    }

    private async Task CreateConsumer(CancellationToken ct)
    {
        var builder = new ConsumerBuilder<string, TEvent>(_consumerConfig)
            .SetValueDeserializer(KafkaDataConverter<TEvent>.Instance);

        using var consumer = builder.Build();
        consumer.Subscribe(_handlerOptions.Topic);
        var name = GetConsumerName(consumer);

        _logger.LogInformation("Consumer {C} for message {M} from topic {T} has been subscribed",
            name, _handlerOptions.EventName, _handlerOptions.Topic);

        ct.Register(() => _logger.LogInformation("Consumer {C} for message {M} from topic {T} stopping...",
            name, _handlerOptions.EventName, _handlerOptions.Topic));

        while (!ct.IsCancellationRequested)
            await ConsumeAsync(consumer, ct);
    }

    private async Task ConsumeAsync(IConsumer<string, TEvent> consumer, CancellationToken ct)
    {
        try
        {
            var result = consumer.Consume(ct);
            if (result.IsPartitionEOF)
                return;

            _logger.LogInformation("Message {M} with key {K} from topic {T} is processed by the consumer {C}...",
                _handlerOptions.EventName, result.Message.Key, _handlerOptions.Topic, consumer.MemberId);

            await using var scope = _serviceFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<TEvent>>();

            await _policy.ExecuteAsync(() => handler.HandleAsync(result.Message.Value));

            _logger.LogInformation("Message {M} with key {K} from topic {T} has been processed by the consumer {C}",
                _handlerOptions.EventName, result.Message.Key, _handlerOptions.Topic, consumer.MemberId);

            consumer.Commit(result);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Consumer {C} has been canceled while processing messages {M} from topic {T}",
                consumer.MemberId, _handlerOptions.EventName, _handlerOptions.Topic);
            consumer.Close();

            throw;
        }
        catch (ConsumeException ex) when (ex.Error.IsFatal)
        {
            _logger.LogCritical(ex, "Consumer {C} threw a fatal exception while processing messages {M} from topic {T}",
                consumer.MemberId, _handlerOptions.EventName, _handlerOptions.Topic);
            _application.StopApplication();

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Consumer {C} threw an exception while processing messages {M} from topic {T}",
                consumer.MemberId, _handlerOptions.EventName, _handlerOptions.Topic);
        }
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
