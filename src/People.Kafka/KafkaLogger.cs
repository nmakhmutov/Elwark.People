using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace People.Kafka;

internal static partial class KafkaLogger
{
    [LoggerMessage(LogLevel.Debug, "Consumer {Consumer} for topic {Topic} has been subscribed")]
    internal static partial void Subscribed(this ILogger logger, string consumer, string topic);

    [LoggerMessage(LogLevel.Information, "Consumer {Consumer} received message {@Message} from topic {Topic}")]
    internal static partial void ReceivedMessage(this ILogger logger, string consumer, object message, string topic);

    [LoggerMessage(LogLevel.Debug, "Consumer {Consumer} handled message {@Message} from topic {Topic}")]
    internal static partial void HandledMessage(this ILogger logger, string consumer, object message, string topic);

    [LoggerMessage(LogLevel.Error, "Error occured in kafka consumer. Retry {Retry} Delay {Delay}")]
    internal static partial void ConsumerException(this ILogger logger, Exception? ex, int retry, TimeSpan delay);

    [LoggerMessage(LogLevel.Critical, "Consumer {Consumer} raised an exception processing {Topic} messages")]
    internal static partial void ConsumerException(this ILogger logger, Exception ex, string consumer, string topic);

    [LoggerMessage(LogLevel.Debug, "Consumer {Consumer} for topic {Topic} has been canceled")]
    internal static partial void ConsumerCanceled(this ILogger logger, string consumer, string topic);

    [LoggerMessage(LogLevel.Debug, "Topic {Topic} created {Specification}")]
    internal static partial void TopicCreated(this ILogger logger, string topic, TopicSpecification specification);

    [LoggerMessage(LogLevel.Debug, "Topic {Topic} already exists")]
    internal static partial void TopicAlreadyExists(this ILogger logger, string topic);

    [LoggerMessage(LogLevel.Critical, "Exception occured while creating topic {Topic}")]
    internal static partial void TopicCannotBeCreated(this ILogger logger, Exception ex, string topic);

    [LoggerMessage(LogLevel.Debug, "Message {@Message} sending")]
    internal static partial void MessageSending(this ILogger logger, object message);

    [LoggerMessage(LogLevel.Debug, "Message {@Message} has been sent")]
    internal static partial void MessageSent(this ILogger logger, object message);

    [LoggerMessage(LogLevel.Critical, "Sending event failed. Retry {Retry} Delay {Delay}")]
    internal static partial void PublisherException(this ILogger logger, Exception? ex, int retry, TimeSpan delay);
}
