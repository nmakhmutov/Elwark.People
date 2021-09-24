using System;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace People.Kafka
{
    public static class ServiceCollectionExtensions
    {
        public static IKafkaBuilder AddKafkaMessageBus(this IServiceCollection services)
        {
            var builder = new KafkaBuilder(services);
            builder.Services.AddSingleton<IKafkaMessageBus, KafkaMessageBus>();

            return builder;
        }

        public static IKafkaBuilder ConfigureProducers(this IKafkaBuilder builder, Action<ProducerConfig> option)
        {
            builder.Services.Configure(option);
            return builder;
        }

        public static IKafkaBuilder ConfigureConsumers(this IKafkaBuilder builder, Action<ConsumerConfig> option)
        {
            builder.Services.Configure(option);
            return builder;
        }

        public static IKafkaBuilder AddProducer<T>(this IKafkaBuilder builder, Action<KafkaProducerConfig<T>> config)
            where T : IKafkaMessage
        {
            builder.Services.Configure(config);
            builder.Services.AddSingleton<KafkaProducer<T>>();
            builder.Services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ProducerConfig>>();
                var logger = sp.GetRequiredService<ILogger<KafkaProducer<T>>>();

                var producer = new ProducerBuilder<Null, T>(options.Value)
                    .SetErrorHandler((_, error) => logger.LogError("Error occured on publishing {R}", error.Reason))
                    .SetValueSerializer(KafkaDataConverter<T>.Instance);

                return producer.Build();
            });

            return builder;
        }

        public static IKafkaBuilder AddConsumer<TMessage, THandler>(this IKafkaBuilder builder,
            Action<KafkaConsumerConfig<TMessage>> config)
            where THandler : class, IKafkaHandler<TMessage>
            where TMessage : IKafkaMessage
        {
            builder.Services.Configure(config);
            builder.Services.AddTransient<IKafkaHandler<TMessage>, THandler>();
            builder.Services.AddHostedService<KafkaConsumer<TMessage, THandler>>();

            return builder;
        }
    }
}
