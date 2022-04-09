using People.Domain.Aggregates.AccountAggregate.Identities;

namespace People.Api.Infrastructure.Provider.Social.Microsoft;

public sealed class MicrosoftAccount
{
    public MicrosoftAccount(Identity.Microsoft identity, Identity.Email email, string? firstName, string? lastName)
    {
        Identity = identity;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }

    public Identity.Microsoft Identity { get; }

    public Identity.Email Email { get; }

    public string? FirstName { get; }

    public string? LastName { get; }
}
