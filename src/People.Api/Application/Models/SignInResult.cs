using People.Domain.AggregateModels.Account;

namespace People.Api.Application.Models
{
    public sealed record SignInResult(AccountId Id, string FullName);
}
