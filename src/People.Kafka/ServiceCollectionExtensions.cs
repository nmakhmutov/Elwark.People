using Confluent.Kafka;
using HealthChecks.Kafka;
using Microsoft.Extensions.Logging;
using People.Kafka;
using People.Kafka.Configurations;
using People.Kafka.Consumers;
using People.Kafka.Integration;
using People.Kafka.Producers;

// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHealthChecksBuilder AddKafka(this IHealthChecksBuilder builder, string servers, string appName) =>
        builder.AddKafka(new KafkaHealthCheckOptions
        {
            Configuration = new ProducerConfig
            {
                BootstrapServers = servers,
                ClientId = appName,
                Acks = Acks.Leader
            },
            MessageBuilder = _ => new Message<string, string>
            {
                Key = appName,
                Value = string.Empty
            }
        });

    public static IKafkaBuilder AddKafka(this IServiceCollection services, string servers)
    {
        var builder = new KafkaBuilder(services, servers);

        builder.Services
            .AddSingleton<IIntegrationEventBus, KafkaEventBus>();

        return builder;
    }

    public static IKafkaBuilder AddProducer<T>(
        this IKafkaBuilder builder,
        Action<ProducerConfigurationBuilder> producer
    )
        where T : IIntegrationEvent
    {
        builder.Services
            .AddSingleton<IKafkaProducer<T>>(sp =>
            {
                var configuration = new ProducerConfigurationBuilder();
                producer.Invoke(configuration);

                configuration.WithServers(builder.Brokers);

                var logger = sp.GetRequiredService<ILogger<IKafkaProducer<T>>>();

                return new KafkaProducer<T>(configuration.Build(), logger);
            });

        return builder;
    }

    public static IKafkaBuilder AddConsumer<TEvent, THandler>(
        this IKafkaBuilder builder,
        Action<ConsumerConfigurationBuilder> consumer
    )
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        builder.Services
            .AddTransient<IIntegrationEventHandler<TEvent>, THandler>()
            .AddHostedService<KafkaConsumer<TEvent, THandler>>(sp =>
            {
                var configuration = new ConsumerConfigurationBuilder();
                consumer.Invoke(configuration);

                configuration.WithServers(builder.Brokers);

                var factory = sp.GetRequiredService<IServiceScopeFactory>();
                var logger = sp.GetRequiredService<ILoggerFactory>();

                return new KafkaConsumer<TEvent, THandler>(configuration.Build(), factory, logger);
            });

        return builder;
    }
}
