using Microsoft.AspNetCore.Authorization;

namespace People.Api.Infrastructure;

internal sealed record PolicyRule(string Name, AuthorizationPolicy Policy);

internal static class Policy
{
    public static readonly PolicyRule RequireAuthenticatedUser = new(
        nameof(RequireAuthenticatedUser),
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build()
    );

    public static readonly PolicyRule RequireCommonAccess = new(
        nameof(RequireCommonAccess),
        new AuthorizationPolicyBuilder()
            .RequireAssertion(context => context.User.FindAll("scope").SelectMany(x => x.Value.Split(' ')).Contains("elwark.people"))
            .Build()
    );

    public static readonly PolicyRule RequireProfileAccess = new(
        nameof(RequireProfileAccess),
        new AuthorizationPolicyBuilder()
            .RequireAssertion(context => context.User.FindAll("scope").SelectMany(x => x.Value.Split(' ')).Contains("elwark.people.profile"))
            .Build()
    );

    public static readonly PolicyRule RequireManagementAccess = new(
        nameof(RequireManagementAccess),
        new AuthorizationPolicyBuilder()
            .RequireAssertion(context => context.User.FindAll("scope").SelectMany(x => x.Value.Split(' ')).Contains("elwark.people.admin"))
            .RequireRole("adm", "ppl.adm")
            .Build()
    );
}
