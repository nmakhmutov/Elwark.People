using System.Diagnostics;
using System.Text;
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

namespace People.Kafka.Consumers;

internal sealed class KafkaConsumer<TEvent, THandler> : IHostedLifecycleService
    where TEvent : IIntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    private readonly ConsumerConfiguration _configuration;
    private readonly ILogger<KafkaConsumer<TEvent, THandler>> _logger;
    private readonly ResiliencePipeline _policy;
    private readonly IServiceScopeFactory _serviceFactory;

    public KafkaConsumer(
        ConsumerConfiguration configuration,
        IServiceScopeFactory serviceFactory,
        ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _serviceFactory = serviceFactory;
        _logger = loggerFactory.CreateLogger<KafkaConsumer<TEvent, THandler>>();

        _policy = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = configuration.RetryCount,
                Delay = configuration.RetryInterval,
                UseJitter = true,
                OnRetry = x =>
                {
                    _logger.ConsumerException(x.Outcome.Exception, x.AttemptNumber, x.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public Task StartingAsync(CancellationToken cancellationToken) =>
        _configuration.TopicSpecification is null
            ? Task.CompletedTask
            : CreateTopicIfNotExistsAsync(_configuration.TopicSpecification);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var tasks = new Task[_configuration.Workers];
        for (var i = 0; i < _configuration.Workers; i++)
            tasks[i] = CreateConsumer(cancellationToken);

        return Task.WhenAll(tasks);
    }

    public Task StartedAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private async Task CreateConsumer(CancellationToken ct)
    {
        using var consumer = new ConsumerBuilder<Guid, TEvent>(_configuration.Config)
            .SetKeyDeserializer(KafkaKeyConverter.Instance)
            .SetValueDeserializer(KafkaValueConverter<TEvent>.Instance)
            .Build();

        consumer.Subscribe(_configuration.Topic);
        while (string.IsNullOrEmpty(consumer.MemberId))
            await Task.Delay(100, ct);

        _logger.Subscribed(consumer.MemberId, _configuration.Topic);

        while (!ct.IsCancellationRequested)
            await ConsumeAsync(consumer, ct);
    }

    private async Task ConsumeAsync(IConsumer<Guid, TEvent> consumer, CancellationToken ct)
    {
        try
        {
            var result = consumer.Consume(ct);
            if (result.IsPartitionEOF)
                return;

            SetActivity(result.Message.Headers);

            _logger.ReceivedMessage(consumer.MemberId, result.Message.Value, _configuration.Topic);

            await using (var scope = _serviceFactory.CreateAsyncScope())
            {
                var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<TEvent>>();
                await _policy.ExecuteAsync(async token => await handler.HandleAsync(result.Message.Value, token), ct);

                _logger.HandledMessage(consumer.MemberId, result.Message.Value, _configuration.Topic);
            }

            consumer.Commit(result);
        }
        catch (ConsumeException ex) when (ex.Error.IsFatal)
        {
            _logger.ConsumerException(ex, consumer.MemberId, _configuration.Topic);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.ConsumerCanceled(consumer.MemberId, _configuration.Topic);
        }
        catch (Exception ex)
        {
            _logger.ConsumerException(ex, consumer.MemberId, _configuration.Topic);
        }
    }

    private static void SetActivity(Headers headers)
    {
        Activity.Current ??= new Activity(nameof(KafkaConsumer<TEvent, THandler>)).Start();

        if (!headers.TryGetLastBytes(nameof(Activity.TraceId), out var traceIdBytes))
            return;

        if (!headers.TryGetLastBytes(nameof(Activity.SpanId), out var spanIdBytes))
            return;

        var traceId = ActivityTraceId.CreateFromString(Encoding.UTF8.GetString(traceIdBytes));
        var spanId = ActivitySpanId.CreateFromString(Encoding.UTF8.GetString(spanIdBytes));

        Activity.Current.SetParentId(traceId, spanId);
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
            await client.CreateTopicsAsync([specification]);

            _logger.TopicCreated(specification.Name, specification);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.TopicAlreadyExists(specification.Name);
        }
        catch (Exception ex)
        {
            _logger.TopicCannotBeCreated(ex, specification.Name);
            throw;
        }
    }
}
