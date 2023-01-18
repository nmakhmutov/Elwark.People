using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using People.Infrastructure.Integration;
using People.Infrastructure.Kafka;
using People.Infrastructure.Kafka.Configurations;
using People.Infrastructure.Kafka.Converters;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private static IKafkaBuilder AddKafkaMessageBus(this IServiceCollection services, Action<ProducerConfig> producer,
        Action<ConsumerConfig> consumer)
    {
        var builder = new KafkaBuilder(services);
        builder.Services
            .Configure(producer)
            .Configure(consumer)
            .AddSingleton<IIntegrationEventBus, KafkaEventBus>();

        return builder;
    }

    public static IKafkaBuilder AddKafkaMessageBus(this IServiceCollection services, string appName, string servers)
    {
        void Producer(ProducerConfig config)
        {
            config.BootstrapServers = servers;
            config.Acks = Acks.All;
        }

        void Consumer(ConsumerConfig config)
        {
            config.BootstrapServers = servers;

            config.GroupId = appName;
            config.AutoOffsetReset = AutoOffsetReset.Earliest;
            config.EnableAutoCommit = false;
            config.EnablePartitionEof = false;
        }

        return AddKafkaMessageBus(services, Producer, Consumer);
    }

    public static IKafkaBuilder AddProducer<T>(this IKafkaBuilder builder, Action<KafkaProducerConfig<T>> config)
        where T : IIntegrationEvent
    {
        builder.Services
            .Configure(config)
            .AddSingleton<IKafkaProducer<T>, KafkaProducer<T>>()
            .AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ProducerConfig>>();
                var logger = sp.GetRequiredService<ILogger<IKafkaProducer<T>>>();

                var producer = new ProducerBuilder<string, T>(options.Value)
                    .SetErrorHandler((_, error) => logger.LogError("Error occured on publishing {R}", error.Reason))
                    .SetKeySerializer(KafkaKeyConverter.Instance)
                    .SetValueSerializer(KafkaDataConverter<T>.Instance);

                return producer.Build();
            });

        return builder;
    }

    public static IKafkaBuilder AddConsumer<E, H>(this IKafkaBuilder builder, Action<KafkaConsumerConfig<E>> config)
        where E : IIntegrationEvent
        where H : class, IIntegrationEventHandler<E>
    {
        builder.Services
            .Configure(config)
            .AddTransient<IIntegrationEventHandler<E>, H>()
            .AddHostedService<KafkaConsumer<E, H>>();

        return builder;
    }
}
