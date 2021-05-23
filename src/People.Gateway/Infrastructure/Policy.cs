using Microsoft.AspNetCore.Authorization;

namespace People.Gateway.Infrastructure
{
    public static class Policy
    {
        public const string RequireAccountId = nameof(RequireAccountId);
        public static AuthorizationPolicy RequireAccountIdPolicy() =>
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

        public const string RequireCommonAccess = nameof(RequireCommonAccess);
        public static AuthorizationPolicy RequireCommonAccessPolicy() =>
            new AuthorizationPolicyBuilder()
                .RequireClaim("scope", "elwark.people")
                .Build();

        public const string RequireProfileAccess = nameof(RequireProfileAccess);
        public static AuthorizationPolicy RequireProfileAccessPolicy() =>
            new AuthorizationPolicyBuilder()
                .RequireClaim("scope", "elwark.people.profile")
                .Build();
    }
}
