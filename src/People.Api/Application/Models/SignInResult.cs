using People.Domain.Aggregates.Account;

namespace People.Api.Application.Models
{
    public sealed record SignInResult(AccountId Id, string FullName);
}
