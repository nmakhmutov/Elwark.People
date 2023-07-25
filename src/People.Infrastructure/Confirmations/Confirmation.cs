using People.Domain.Entities;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedMember.Local

namespace People.Infrastructure.Confirmations;

public sealed class Confirmation
{
    private Confirmation()
    {
        Code = string.Empty;
        Type = string.Empty;
    }

    public Confirmation(Guid id, AccountId accountId, string code, string type, DateTime createdAt, TimeSpan ttl)
    {
        Id = id;
        AccountId = accountId;
        Code = code;
        Type = type;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + ttl;
    }

    public Guid Id { get; private set; }

    public AccountId AccountId { get; private set; }

    public string Code { get; private set; }

    public string Type { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }
}
