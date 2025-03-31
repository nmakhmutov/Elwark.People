using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace People.Kafka;

internal static partial class KafkaLogger
{
    [LoggerMessage(LogLevel.Debug, "Consumer {Consumer} for topic {Topic} has been subscribed")]
    internal static partial void Subscribed(this ILogger logger, string consumer, string topic);

    [LoggerMessage(LogLevel.Information, "Message received: {@Message} from topic {Topic}, via publisher {Client}")]
    internal static partial void MessageReceived(this ILogger logger, object message, string topic, string client);

    [LoggerMessage(LogLevel.Debug,
        "Successfully handled message: {@Message} from topic {Topic}, via publisher {Client}")]
    internal static partial void MessageHandled(this ILogger logger, object message, string topic, string client);

    [LoggerMessage(LogLevel.Error,
        "Message {@Message} could not be processed from topic {Topic} via publisher {Client}")]
    public static partial void MessageFailed(
        this ILogger logger,
        Exception exception,
        object message,
        string topic,
        string client
    );

    [LoggerMessage(LogLevel.Error, "Error occurred while handling message from topic {Topic}. Retry {Retry}")]
    internal static partial void MessageFailed(this ILogger logger, Exception? ex, string topic, int retry);

    [LoggerMessage(LogLevel.Critical, "Consumer {Consumer} raised an exception processing {Topic} messages")]
    internal static partial void ConsumerException(this ILogger logger, Exception ex, string consumer, string topic);

    [LoggerMessage(LogLevel.Debug, "Consumer {Consumer} for topic {Topic} has been unsubscribed")]
    internal static partial void ConsumerCanceled(this ILogger logger, string consumer, string topic);

    [LoggerMessage(LogLevel.Debug, "Topic {Topic} created {@Specification}")]
    internal static partial void TopicCreated(this ILogger logger, string topic, TopicSpecification specification);

    [LoggerMessage(LogLevel.Debug, "Topic {Topic} already exists")]
    internal static partial void TopicAlreadyExists(this ILogger logger, string topic);

    [LoggerMessage(LogLevel.Critical, "Exception occurred while creating topic {Topic}")]
    internal static partial void TopicCannotBeCreated(this ILogger logger, Exception ex, string topic);

    [LoggerMessage(LogLevel.Debug, "Dispatching message {@Message}")]
    internal static partial void DispatchingMessage(this ILogger logger, object message);

    [LoggerMessage(LogLevel.Information, "Dispatched message {@Message}")]
    internal static partial void DispatchedMessage(this ILogger logger, object message);

    [LoggerMessage(LogLevel.Error, "Sending event failed. Retry {Retry} Delay {Delay}")]
    internal static partial void PublisherException(this ILogger logger, Exception? ex, int retry, TimeSpan delay);

    [LoggerMessage(LogLevel.Error, "Error {Message} occurred on kafka publisher. {@Error}")]
    internal static partial void PublisherException(this ILogger logger, string message, Error error);

    [LoggerMessage(LogLevel.Error, "Error {Message} occurred on kafka consumer. {@Error}")]
    internal static partial void ConsumerException(this ILogger logger, string message, Error error);
}
