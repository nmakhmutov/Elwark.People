namespace People.Domain.Exceptions;

public sealed class AccountException : PeopleException
{
    private AccountException(string code, long id, string? message = null)
        : base(nameof(AccountException), code, message) =>
        Id = id;

    public long Id { get; }

    public static AccountException NotFound(long id) =>
        new(nameof(NotFound), id, $"Account '{id}' not found");

    public static AccountException PrimaryEmailCannotBeRemoved(long id) =>
        new(nameof(PrimaryEmailCannotBeRemoved), id, $"Primary email for account {id} cannot be removed");
}
