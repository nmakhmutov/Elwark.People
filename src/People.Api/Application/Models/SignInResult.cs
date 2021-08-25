using People.Domain.Aggregates.AccountAggregate;

namespace People.Api.Application.Models
{
    public sealed record SignInResult(AccountId Id, string FullName);
}
