using People.Domain.Aggregates.Account.Identities;

namespace People.Api.Infrastructure.Provider.Social.Microsoft
{
    public class MicrosoftAccount
    {
        public MicrosoftAccount(MicrosoftIdentity identity, EmailIdentity email, string? firstName, string? lastName)
        {
            Identity = identity;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
        }

        public MicrosoftIdentity Identity { get; }
        
        public EmailIdentity Email { get; }

        public string? FirstName { get; }

        public string? LastName { get; }
    }
}