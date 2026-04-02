using System.Text.Json;
using People.Domain;
using People.Domain.Events;

namespace People.Infrastructure.Outbox.Entities;

public sealed class OutboxMessage
{
    public const int MaxErrorLength = 256;
    public const int MaxAttempts = 25;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private readonly string _payload;
    private readonly string _type;
    private int _attempts;

    // ReSharper disable once NotAccessedField.Local
    private string? _error;

    public Guid Id { get; }

    public OutboxStatus Status { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public DateTime? NextRetryAt { get; private set; }

    // ReSharper disable once UnusedMember.Local
    private OutboxMessage()
    {
        Id = Guid.Empty;
        _type = string.Empty;
        _payload = string.Empty;
    }

    private OutboxMessage(Guid id, string type, string payload, DateTime occurredAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Outbox message id cannot be empty.", nameof(id));

        if (type.HasNoValue())
            throw new ArgumentException("Outbox message type is required.", nameof(type));

        if (payload.HasNoValue())
            throw new ArgumentException("Outbox message payload is required.", nameof(payload));

        Id = id;
        _type = type.Trim();
        _payload = payload;
        OccurredAt = occurredAt;
        Status = OutboxStatus.Created;
    }

    public static OutboxMessage Create(IIntegrationEvent payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var type = payload.GetType();
        var json = JsonSerializer.Serialize(payload, type, Options);

        return new OutboxMessage(payload.Id, type.AssemblyQualifiedName!, json, payload.OccurredAt);
    }

    public void MarkProcessed(DateTime processedAt)
    {
        Status = OutboxStatus.Completed;
        ProcessedAt = processedAt;
        _error = null;
        NextRetryAt = null;
    }

    public void MarkFailed(DateTime failedAt, Exception exception)
    {
        _attempts++;

        _error = exception.Message.Length > MaxErrorLength
            ? exception.Message[..MaxErrorLength]
            : exception.Message;

        if (_attempts >= MaxAttempts)
        {
            Status = OutboxStatus.Failed;
            NextRetryAt = null;
            ProcessedAt = failedAt;
        }
        else
        {
            Status = OutboxStatus.Pending;
            NextRetryAt = failedAt.AddSeconds(Math.Min(Math.Max(_attempts, 1) * 15, 300));
        }
    }

    public IIntegrationEvent GetPayload()
    {
        var type = Type.GetType(_type)
            ?? throw new InvalidOperationException($"Cannot resolve outbox payload type '{_type}' for message '{Id}'.");

        var payload = JsonSerializer.Deserialize(_payload, type, Options);

        return payload as IIntegrationEvent
            ?? throw new InvalidOperationException($"Cannot deserialize outbox payload for message '{Id}'.");
    }
}
