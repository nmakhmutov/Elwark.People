using Microsoft.AspNetCore.Authorization;

namespace People.Api.Infrastructure;

internal sealed record PolicyRule(string Name, AuthorizationPolicy Policy);

internal static class Policy
{
    public static PolicyRule RequireAuthenticatedUser = new (
        nameof(RequireAuthenticatedUser),
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build()
    );
    
    public static PolicyRule RequireCommonAccess = new (
        nameof(RequireCommonAccess),
        new AuthorizationPolicyBuilder()
            .RequireClaim("scope", "elwark.people")
            .Build()
    );
    
    public static PolicyRule RequireProfileAccess = new (
        nameof(RequireProfileAccess),
        new AuthorizationPolicyBuilder()
            .RequireClaim("scope", "elwark.people.profile")
            .Build()
    );
    
    public static PolicyRule RequireManagementAccess = new (
        nameof(RequireManagementAccess),
        new AuthorizationPolicyBuilder()
            .RequireClaim("scope", "elwark.people.admin")
            .RequireRole("adm", "ppl.adm")
            .Build()
    );
}
