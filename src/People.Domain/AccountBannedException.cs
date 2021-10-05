using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Domain;

public class AccountBannedException : ElwarkException
{
    public AccountBannedException(Ban ban)
        : base(ElwarkExceptionCodes.AccountBanned, ban.Reason) =>
        Ban = ban;

    public Ban Ban { get; }
}
