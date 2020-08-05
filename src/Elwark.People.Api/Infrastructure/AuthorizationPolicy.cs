using Microsoft.AspNetCore.Authorization;

namespace Elwark.People.Api.Infrastructure
{
    public class Policy
    {
        public const string Common = nameof(Common);

        public const string Account = nameof(Account);

        public const string Identity = nameof(Identity);

        public static AuthorizationPolicy CommonPolicy() =>
            new AuthorizationPolicyBuilder()
                .RequireClaim("scope", "elwark.people.api")
                .Build();

        public static AuthorizationPolicy AccountPolicy() =>
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim("identity")
                .RequireClaim("scope", "elwark.people.api.account")
                .Build();

        public static AuthorizationPolicy IdentityPolicy() =>
            new AuthorizationPolicyBuilder()
                .RequireClaim("scope", "elwark.people.api.identity")
                .Build();
    }
}