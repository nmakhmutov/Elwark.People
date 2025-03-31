using System.Diagnostics;
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
        ILoggerFactory loggerFactory
    )
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
                    _logger.MessageFailed(x.Outcome.Exception, configuration.Topic, x.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public Task StartingAsync(CancellationToken ct) =>
        _configuration.TopicSpecification is null
            ? Task.CompletedTask
            : CreateTopicIfNotExistsAsync(_configuration.TopicSpecification);

    public Task StartAsync(CancellationToken ct) =>
        Task.CompletedTask;

    public Task StartedAsync(CancellationToken ct)
    {
        var tasks = new Task[_configuration.Workers];
        for (var i = 0; i < _configuration.Workers; i++)
            tasks[i] = CreateConsumer(ct);

        return Task.WhenAll(tasks);
    }

    public Task StoppingAsync(CancellationToken ct) =>
        Task.CompletedTask;

    public Task StopAsync(CancellationToken ct) =>
        Task.CompletedTask;

    public Task StoppedAsync(CancellationToken ct) =>
        Task.CompletedTask;

    private async Task CreateConsumer(CancellationToken ct)
    {
        using var consumer = new ConsumerBuilder<string, TEvent>(_configuration.Config)
            .SetLogHandler((_, message) =>
            {
                var level = (LogLevel)message.LevelAs(LogLevelType.MicrosoftExtensionsLogging);

                _logger.Log(level, "Consumer exception {Name} with message {Message}", message.Name, message.Message);
            })
            .SetErrorHandler((_, error) => _logger.ConsumerException(error.Reason, error))
            .SetKeyDeserializer(KafkaKeyConverter.Instance)
            .SetValueDeserializer(KafkaValueConverter<TEvent>.Instance)
            .Build();

        consumer.Subscribe(_configuration.Topic);
        while (string.IsNullOrEmpty(consumer.MemberId))
            await Task.Delay(50, ct);

        _logger.Subscribed(consumer.MemberId, _configuration.Topic);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = consumer.Consume(ct);
                if (result.IsPartitionEOF)
                    continue;

                await HandleMessageAsync(result, ct);

                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.ConsumerCanceled(consumer.MemberId, _configuration.Topic);
        }
        catch (Exception ex)
        {
            _logger.ConsumerException(ex, consumer.MemberId, _configuration.Topic);

            throw;
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(ConsumeResult<string, TEvent> result, CancellationToken ct)
    {
        var context = new ActivityContext(
            result.Message.Headers.GetTraceId(),
            result.Message.Headers.GetSpanId(),
            ActivityTraceFlags.Recorded,
            null,
            true
        );

        var clientId = result.Message.Headers.GetClientId();

        using var activity = KafkaTelemetry.StartConsumerActivity(result.Topic, context);

        activity?.AddTag("kafka.consumer.group.id", _configuration.Config.GroupId)
            .AddTag("kafka.consumer.topic.key", result.Message.Key)
            .AddTag("kafka.producer.client.id", clientId);

        _logger.MessageReceived(result.Message.Value, _configuration.Topic, clientId);

        var outcome = await HandleEventAsync(result.Message.Value, ct);

        if (outcome.Exception is null)
        {
            _logger.MessageHandled(result.Message.Value, _configuration.Topic, clientId);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            _logger.MessageFailed(outcome.Exception, result.Message.Value, _configuration.Topic, clientId);

            activity?.AddException(outcome.Exception);
            activity?.SetStatus(ActivityStatusCode.Error);
        }
    }

    private async Task<Outcome<bool>> HandleEventAsync(TEvent message, CancellationToken ct)
    {
        var context = ResilienceContextPool.Shared.Get(ct);

        var result = await _policy.ExecuteOutcomeAsync(
            async (ctx, state) =>
            {
                await using var scope = _serviceFactory.CreateAsyncScope();

                await scope.ServiceProvider
                    .GetRequiredService<IIntegrationEventHandler<TEvent>>()
                    .HandleAsync(state, ctx.CancellationToken);

                return Outcome.FromResult(true);
            },
            context,
            message
        );

        ResilienceContextPool.Shared.Return(context);

        return result;
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
        catch (CreateTopicsException ex) when (ex.Results.TrueForAll(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
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
