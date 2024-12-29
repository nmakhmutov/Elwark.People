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

                var logger = sp.GetRequiredService<ILogger<IKafkaProducer<T>>>();

                return new KafkaProducer<T>(configuration.Build(builder.Brokers), logger);
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

                var factory = sp.GetRequiredService<IServiceScopeFactory>();
                var logger = sp.GetRequiredService<ILoggerFactory>();

                return new KafkaConsumer<TEvent, THandler>(configuration.Build(builder.Brokers), factory, logger);
            });

        return builder;
    }
}
