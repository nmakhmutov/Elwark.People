using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using People.Infrastructure.Integration;
using Polly;
using Polly.Retry;

namespace People.Infrastructure.Kafka;

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
            .WaitAndRetryAsync(
                _handlerOptions.RetryCount,
                _ => _handlerOptions.RetryInterval,
                (ex, time, retry, _) =>
                    _logger.LogCritical(ex, "Error occured in kafka handler for {N}. Retry {R}. Time {T}",
                        _handlerOptions.EventName, retry, time)
            );
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        switch (_handlerOptions.Threads)
        {
            case 0:
                _logger.LogWarning("{N} consumer doesn't have any threads", _handlerOptions.EventName);
                break;

            case 1:
                await CreateTopicIfNotExistsAsync();
                await Task.Factory.StartNew(() => CreateConsumer(ct), TaskCreationOptions.LongRunning);
                break;

            default:
                await CreateTopicIfNotExistsAsync();

                var tasks = new Task[_handlerOptions.Threads];
                for (var i = 0; i < _handlerOptions.Threads; i++)
                    tasks[i] = Task.Factory.StartNew(() => CreateConsumer(ct), TaskCreationOptions.LongRunning);

                await Task.WhenAll(tasks);
                break;
        }
    }

    private async Task CreateConsumer(CancellationToken ct)
    {
        var builder = new ConsumerBuilder<string, Event>(_consumerConfig)
            .SetValueDeserializer(KafkaDataConverter<Event>.Instance);

        using var consumer = builder.Build();
        consumer.Subscribe(_handlerOptions.Topic);

        var name = consumer.Name;
        var id = consumer.MemberId;

        _logger.LogInformation("Consumer {N} ({I}) for message {M} from topic {T} subscribed", name, id,
            _handlerOptions.EventName, _handlerOptions.Topic);

        ct.Register(() => _logger.LogInformation("Consumer {N} ({I}) for message {M} from topic {T} shutting down...",
            name, id, _handlerOptions.EventName, _handlerOptions.Topic));

        while (!ct.IsCancellationRequested)
            await ConsumeAsync(consumer, ct);
    }

    private async Task ConsumeAsync(IConsumer<string, Event> consumer, CancellationToken ct)
    {
        try
        {
            var result = consumer.Consume(ct);
            if (result.IsPartitionEOF)
                return;

            _logger.LogInformation("Consumer {N} ({I}) received {E} from topic {T}. {M}", consumer.Name,
                consumer.MemberId, _handlerOptions.EventName, _handlerOptions.Topic, result.Message.Value);

            await using var scope = _serviceFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<Event>>();

            await _policy.ExecuteAsync(() => handler.HandleAsync(result.Message.Value));

            _logger.LogInformation("Consumer {N} ({I}) handled {E} from topic {T}. {M}", consumer.Name,
                consumer.MemberId, _handlerOptions.EventName, _handlerOptions.Topic, result.Message.Value);

            consumer.Commit(result);
        }
        catch (OperationCanceledException ex)
        {
            consumer.Close();
            _logger.LogWarning(ex, "Consumer {N} ({I}) for message {M} from topic {T} canceled", 
                consumer.Name, consumer.MemberId, _handlerOptions.EventName, _handlerOptions.Topic);

            throw;
        }
        catch (ConsumeException ex) when (ex.Error.IsFatal)
        {
            _logger.LogCritical(ex, "Consumer {N} ({I}) for message {M} from topic {T} failed. Application stopping",
                consumer.Name, consumer.MemberId, _handlerOptions.EventName, _handlerOptions.Topic);
            _application.StopApplication();

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "Consumer {N} ({I}) for message {M} from topic {T} failed. Unhandled exception has occured",
                consumer.Name, consumer.MemberId, _handlerOptions.EventName, _handlerOptions.Topic);
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
            _logger.LogCritical(ex, "Exception occured on kafka topic creation");
            _application.StopApplication();

            throw;
        }
    }
}
