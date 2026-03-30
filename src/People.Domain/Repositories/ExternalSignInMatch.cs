using People.Domain.Entities;
using People.Domain.ValueObjects;

namespace People.Domain.Repositories;

public sealed record ExternalSignInMatch(AccountId Id, Name Name);
