// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedMember.Local

namespace People.Infrastructure.Confirmations;

public sealed class Confirmation
{
    private Confirmation() =>
        Type = string.Empty;

    public Confirmation(Guid id, long accountId, int code, string type, DateTime createdAt, TimeSpan ttl)
    {
        Id = id;
        AccountId = accountId;
        Code = code;
        Type = type;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + ttl;
    }

    public Guid Id { get; private set; }

    public long AccountId { get; private set; }

    public int Code { get; private set; }

    public string Type { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }
}
