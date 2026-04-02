namespace People.Infrastructure.Outbox.Entities;

public sealed class OutboxConsumer
{
    // ReSharper disable once UnusedMember.Local
    private OutboxConsumer() =>
        Consumer = string.Empty;

    private OutboxConsumer(Guid messageId, string consumer, DateTime processedAt)
    {
        MessageId = messageId;
        Consumer = consumer;
        ProcessedAt = processedAt;
    }

    public Guid MessageId { get; }

    public string Consumer { get; }

    public DateTime ProcessedAt { get; }

    public static OutboxConsumer Create(Guid outboxMessageId, string consumerName, DateTime processedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        return new OutboxConsumer(outboxMessageId, consumerName, processedAt);
    }
}
