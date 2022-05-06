using Common.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            .AddSingleton<IIntegrationEventBus, KafkaMessageBus>();

        return builder;
    }

    public static IKafkaBuilder AddKafkaMessageBus(this IServiceCollection services, string appName, string servers) =>
        AddKafkaMessageBus(
            services,
            config =>
            {
                config.BootstrapServers = servers;
                config.Acks = Acks.All;
            },
            config =>
            {
                config.BootstrapServers = servers;

                config.GroupId = appName;
                config.AutoOffsetReset = AutoOffsetReset.Earliest;
                config.EnableAutoCommit = false;
                config.EnablePartitionEof = false;
            }
        );

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

    public static IKafkaBuilder AddConsumer<Event, Handler>(this IKafkaBuilder builder,
        Action<KafkaConsumerConfig<Event>> config)
        where Event : IIntegrationEvent
        where Handler : class, IKafkaHandler<Event>
    {
        builder.Services
            .Configure(config)
            .AddTransient<IKafkaHandler<Event>, Handler>()
            .AddHostedService<KafkaConsumer<Event, Handler>>();

        return builder;
    }
}
