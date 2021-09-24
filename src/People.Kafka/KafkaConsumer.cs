using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace People.Kafka
{
    public sealed class KafkaConsumer<TMessage, THandler> : BackgroundService
        where TMessage : IKafkaMessage
        where THandler : class, IKafkaHandler<TMessage>
    {
        private readonly IHostApplicationLifetime _application;
        private readonly ConsumerBuilder<Null, TMessage> _builder;
        private readonly KafkaConsumerConfig<TMessage> _config;
        private readonly ILogger<KafkaConsumer<TMessage, THandler>> _logger;
        private readonly AsyncRetryPolicy _policy;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public KafkaConsumer(IServiceScopeFactory serviceScopeFactory, IHostApplicationLifetime application,
            ILogger<KafkaConsumer<TMessage, THandler>> logger, IOptions<ConsumerConfig> config,
            IOptions<KafkaConsumerConfig<TMessage>> consumerConfig)
        {
            _logger = logger;
            _application = application;
            _serviceScopeFactory = serviceScopeFactory;
            _config = consumerConfig.Value;
            _builder = new ConsumerBuilder<Null, TMessage>(config.Value)
                .SetValueDeserializer(KafkaDataConverter<TMessage>.Instance);

            _policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(_config.RetryCount, _ => _config.RetryInterval, (ex, time, retry, _) =>
                {
                    var level = retry > _config.RetryCount * 0.5
                        ? LogLevel.Critical
                        : LogLevel.Warning;

                    _logger.Log(level, ex, "Error occured in kafka handler for '{N}'. Retry {retry}. Time {time}",
                        _config.MessageType.Name, retry, time);
                });
        }

        protected override Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Consumer for '{N}' starting...", _config.MessageType.Name);
            ct.Register(() => _logger.LogInformation("Consumer for '{N}' shutting down...", _config.MessageType.Name));

            var consumers = Enumerable.Range(0, _config.Threads)
                .Select(_ => Task.Run(() => CreateConsumer(_builder, ct).ConfigureAwait(false), ct))
                .ToArray();

            return Task.WhenAll(consumers);
        }

        private async Task CreateConsumer(ConsumerBuilder<Null, TMessage> builder, CancellationToken ct)
        {
            using var consumer = builder.Build();
            consumer.Subscribe(_config.Topic);

            _logger.LogInformation("Consumer for '{N}' handling by '{C}' from topic '{T}'",
                _config.MessageType.Name,
                consumer.Name,
                _config.Topic
            );

            while (!ct.IsCancellationRequested)
                try
                {
                    var result = consumer.Consume(ct);
                    if (result.IsPartitionEOF)
                        continue;

                    _logger.LogInformation("Consumer '{N}' received event '{E}' from topic '{T}'. {M}", consumer.Name,
                        _config.MessageType.Name, _config.Topic, result.Message.Value);

                    await _policy.ExecuteAsync(async () =>
                        {
                            await using var scope = _serviceScopeFactory.CreateAsyncScope();
                            var handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<TMessage>>();

                            await handler.HandleAsync(result.Message.Value)
                                .ConfigureAwait(false);
                        })
                        .ConfigureAwait(false);

                    _logger.LogInformation("Consumer '{N}' handled event '{E}' from topic '{T}'", consumer.Name,
                        _config.MessageType.Name, _config.Topic);

                    consumer.Commit(result);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning(ex, "Consumer '{N}' for message '{M}' canceled", consumer.Name,
                        _config.MessageType.Name);

                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Consumer exception in '{N}' for message '{M}'", consumer.Name,
                        _config.MessageType.Name);

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
    }
}
