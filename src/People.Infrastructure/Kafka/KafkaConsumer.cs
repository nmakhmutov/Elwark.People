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

namespace People.Infrastructure.Kafka
{
    public class KafkaConsumer<T> : BackgroundService where T : IKafkaMessage
    {
        private readonly KafkaConsumerConfig<T> _config;
        private readonly ILogger<KafkaConsumer<T>> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHostApplicationLifetime _application;
        private readonly ConsumerBuilder<Null, T> _builder;
        private readonly AsyncRetryPolicy _policy;

        public KafkaConsumer(IServiceScopeFactory serviceScopeFactory, ILogger<KafkaConsumer<T>> logger,
            IOptions<ConsumerConfig> config, IOptions<KafkaConsumerConfig<T>> consumerConfig,
            IHostApplicationLifetime application)
        {
            _logger = logger;
            _application = application;
            _serviceScopeFactory = serviceScopeFactory;
            _config = consumerConfig.Value;
            _builder = new ConsumerBuilder<Null, T>(config.Value)
                // .SetErrorHandler((consumer, error) =>
                // {
                //     var level = error.IsFatal ? LogLevel.Critical : LogLevel.Error;
                //
                //     logger.Log(level, "Consumer {N}. Code {C}. Reason {R}", consumer.Name, error.Code, error.Reason);
                // })
                // .SetLogHandler((consumer, error) =>
                // {
                //     var level = (LogLevel) error.LevelAs(LogLevelType.MicrosoftExtensionsLogging);
                //
                //     logger.Log(level, "Consumer {N}. Code {C}. Message {M}", consumer.Name, error.Name, error.Message);
                // })
                .SetValueDeserializer(KafkaDataConverter<T>.Instance);

            _policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(_config.RetryCount,
                    i => _config.RetryInterval * i,
                    (ex, _, retry, _) =>
                    {
                        var level = retry > _config.RetryCount * 0.7
                            ? LogLevel.Critical
                            : LogLevel.Warning;

                        _logger.Log(level, ex, "Error occured in kafka handler for '{N}'", _config.MessageType.Name);
                    });
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Consumer for '{N}' starting...", _config.MessageType.Name);
            stoppingToken.Register(() =>
                _logger.LogInformation("Consumer for '{N}' shutting down...", _config.MessageType.Name));

            var consumers = Enumerable.Range(0, _config.Threads)
                .Select(_ =>
                    Task.Run(() => CreateConsumerTask(_builder, stoppingToken).ConfigureAwait(false),
                        stoppingToken
                    )
                )
                .ToArray();

            return Task.WhenAll(consumers);
        }

        private async Task CreateConsumerTask(ConsumerBuilder<Null, T> builder, CancellationToken ct)
        {
            using var consumer = builder.Build();
            consumer.Subscribe(_config.Topic);

            _logger.LogInformation("Consumer for '{N}' handling by '{C}' on topic '{T}'",
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

                    _logger.LogInformation("Consumer '{N}' received event '{E}'", consumer.Name,
                        _config.MessageType.Name);

                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<T>>();

                        await _policy.ExecuteAsync(() => handler.HandleAsync(result.Message.Value))
                            .ConfigureAwait(false);
                    }

                    _logger.LogInformation("Consumer '{N}' handled event '{E}'", consumer.Name,
                        _config.MessageType.Name);

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
                    _logger.LogCritical(ex, "Consumer exception in '{N}' for message '{M}'", consumer.Name,
                        _config.MessageType.Name);

                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical(ex, "Consumer exception is fatal. Application will stop");
                        _application.StopApplication();
                        break;
                    }
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