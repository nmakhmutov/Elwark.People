using People.Account.Domain.Aggregates.AccountAggregate;

namespace People.Account.Api.Application.Models
{
    public sealed record SignInResult(AccountId Id, string FullName);
}
