using Microsoft.AspNetCore.Authorization;

namespace People.Api.Infrastructure;

internal sealed record PolicyRule(string Name, AuthorizationPolicy Policy);

internal static class Policy
{
    public static readonly PolicyRule RequireRead = new(
        nameof(RequireRead),
        new AuthorizationPolicyBuilder()
            .RequireClaim("scope", "people:read")
            .Build()
    );

    public static readonly PolicyRule RequireWrite = new(
        nameof(RequireWrite),
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim("scope", "people:write")
            .Build()
    );

    public static readonly PolicyRule RequireAdmin = new(
        nameof(RequireAdmin),
        new AuthorizationPolicyBuilder()
            .RequireClaim("scope", "people:admin")
            .RequireRole("adm", "ppl.adm")
            .Build()
    );
}
