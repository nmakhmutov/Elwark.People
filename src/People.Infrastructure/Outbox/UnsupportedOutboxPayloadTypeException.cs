namespace Shared.Outbox;

public sealed class UnsupportedOutboxPayloadTypeException : InvalidOperationException
{
    public UnsupportedOutboxPayloadTypeException(Type payloadType)
        : base($"Unsupported outbox payload type '{payloadType.FullName}'")
    {
    }
}
