using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;

namespace People.Account.Api.Application.Models
{
    public sealed record SignUpResult(AccountId Id, string FullName, EmailConnection Connection);
}
