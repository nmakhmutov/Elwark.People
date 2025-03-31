using System.Diagnostics;

namespace People.Kafka;

public static class KafkaTelemetry
{
    public const string ActivityName = "Elwark.EventBus.Kafka";

    private static readonly ActivitySource ActivitySource = new(ActivityName, "1.0.0");

    internal static Activity? StartConsumerActivity(string topic, ActivityContext context) =>
        ActivitySource.StartActivity($"Consume {topic}", ActivityKind.Consumer, context);

    internal static Activity? StartProducerActivity(string topic, ActivityContext context) =>
        ActivitySource.StartActivity($"Produce {topic}", ActivityKind.Producer, context);
}
