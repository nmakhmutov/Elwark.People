using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;

namespace People.Api.Application.Models;

public sealed record SignUpResult(AccountId Id, string FullName, EmailConnection Connection);
