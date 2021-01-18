using Microsoft.AspNetCore.Authorization;

namespace People.Gateway.Infrastructure
{
    public class Policy
    {
        public const string RequireAccountId = nameof(RequireAccountId);

        public static AuthorizationPolicy RequireAccountIdPolicy() =>
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
    }
}