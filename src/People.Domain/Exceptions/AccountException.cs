using People.Domain.Entities;

namespace People.Domain.Exceptions;

public sealed class AccountException : PeopleException
{
    private AccountException(string code, AccountId id, string? message = null)
        : base(nameof(AccountException), code, message) =>
        Id = id;

    public AccountId Id { get; }

    public static AccountException NotFound(AccountId id) =>
        new(nameof(NotFound), id, $"Account '{id}' not found");

    public static AccountException PrimaryEmailCannotBeRemoved(AccountId id) =>
        new(nameof(PrimaryEmailCannotBeRemoved), id, $"Primary email for account {id} cannot be removed");
}
