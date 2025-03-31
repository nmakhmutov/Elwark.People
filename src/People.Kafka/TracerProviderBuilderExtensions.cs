using People.Kafka;

// ReSharper disable CheckNamespace

namespace OpenTelemetry.Trace;

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddKafkaInstrumentation(this TracerProviderBuilder builder) =>
        builder.AddSource(KafkaTelemetry.ActivityName);
}
