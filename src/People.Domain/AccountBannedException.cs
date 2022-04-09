using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Domain;

public sealed class AccountBannedException : PeopleException
{
    public AccountBannedException(Ban ban)
        : base(ExceptionCodes.AccountBanned, ban.Reason) =>
        Ban = ban;

    public Ban Ban { get; }
}
