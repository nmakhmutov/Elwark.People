using Microsoft.Extensions.Hosting;
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
            .Configure<HostOptions>(options =>
            {
                options.ServicesStartConcurrently = true;
                options.ServicesStopConcurrently = true;
            })
            .AddSingleton<IIntegrationEventBus, KafkaEventBus>();

        return builder;
    }

    public static IKafkaBuilder AddProducer<T>(this IKafkaBuilder builder,
        Action<ProducerConfigurationBuilder> producer)
        where T : IIntegrationEvent
    {
        var configuration = new ProducerConfigurationBuilder();
        producer.Invoke(configuration);

        builder.Services
            .AddSingleton<IKafkaProducer<T>>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IKafkaProducer<T>>>();
                return new KafkaProducer<T>(configuration.Build(builder.Brokers), logger);
            });

        return builder;
    }

    public static IKafkaBuilder AddConsumer<E, H>(this IKafkaBuilder builder,
        Action<ConsumerConfigurationBuilder> consumer)
        where E : IIntegrationEvent
        where H : class, IIntegrationEventHandler<E>
    {
        var configuration = new ConsumerConfigurationBuilder();
        consumer.Invoke(configuration);

        builder.Services
            .AddTransient<IIntegrationEventHandler<E>, H>()
            .AddHostedService(sp =>
            {
                var factory = sp.GetRequiredService<IServiceScopeFactory>();
                var logger = sp.GetRequiredService<ILoggerFactory>();

                return new KafkaConsumer<E, H>(configuration.Build(builder.Brokers), factory, logger);
            })
            .AddHostedService<KafkaConsumer<E, H>>();

        return builder;
    }
}
