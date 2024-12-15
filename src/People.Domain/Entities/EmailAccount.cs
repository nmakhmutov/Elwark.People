using People.Domain.SeedWork;

// ReSharper disable NotAccessedField.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Entities;

public sealed class EmailAccount : Entity<Guid>
{
    private DateTime? _confirmedAt;
    private DateTime _createdAt;

    public EmailAccount(AccountId accountId, string email, bool isPrimary, DateTime? confirmedAt, DateTime createdAt)
    {
        Id = Guid.CreateVersion7();
        AccountId = accountId;
        Email = email;
        IsPrimary = isPrimary;
        _confirmedAt = confirmedAt;
        _createdAt = createdAt;
    }

    public AccountId AccountId { get; private set; }

    public string Email { get; private set; }

    public bool IsPrimary { get; private set; }

    public bool IsConfirmed =>
        _confirmedAt.HasValue;

    internal bool SetPrimary() =>
        IsPrimary = true;

    internal bool RemovePrimary() =>
        IsPrimary = false;

    internal void Confirm(DateTime confirmedAt) =>
        _confirmedAt = confirmedAt;

    internal void Confute() =>
        _confirmedAt = null;
}
