namespace People.Domain.AggregateModels.Account.Identities
{
    public abstract record Identity(IdentityType Type, string Value);

    public sealed record EmailIdentity(string Email) : Identity(IdentityType.Email, Email.ToLowerInvariant());

    public sealed record GoogleIdentity(string Id) : Identity(IdentityType.Google, Id);

    public sealed record FacebookIdentity(string Id) : Identity(IdentityType.Facebook, Id);

    public sealed record MicrosoftIdentity(string Id) : Identity(IdentityType.Microsoft, Id);
}