using People.Application.Providers.Confirmation;
using People.Domain.Entities;

// ReSharper disable UnusedMember.Local

namespace People.Infrastructure.Confirmations;

public sealed class Confirmation
{
    public Guid Id { get; private set; }

    public AccountId AccountId { get; private set; }

    public ConfirmationType Type { get; private set; }

    public string Code { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Confirmation()
    {
        Code = string.Empty;
        Type = ConfirmationType.EmailConfirmation;
    }

    public Confirmation(AccountId accountId, string code, ConfirmationType type, TimeSpan ttl, DateTime createdAt)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        Code = code;
        Type = type;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + ttl;
    }
}
