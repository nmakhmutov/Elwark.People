using Microsoft.AspNetCore.Authorization;

namespace Gateway.Api.Infrastructure;

public static class Policy
{
    public const string RequireAccountId = "6CEB1D531EAD4155B07D119C308F74CE";
    public const string RequireCommonAccess = "3D4749DA234D44C8A947626E57F40C1A";
    public const string RequireProfileAccess = "EC8A820FBA3340FAA5BF25DB5572E70C";
    public const string ManagementAccess = "A3F423E025964CAD892BC1F6DD81BC49";

    public static AuthorizationPolicy RequireAccountIdPolicy() =>
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

    public static AuthorizationPolicy RequireCommonAccessPolicy() =>
        new AuthorizationPolicyBuilder()
            .RequireClaim("scope", "elwark.people")
            .Build();

    public static AuthorizationPolicy RequireProfileAccessPolicy() =>
        new AuthorizationPolicyBuilder()
            .RequireClaim("scope", "elwark.people.profile")
            .Build();

    public static AuthorizationPolicy ManagementAccessPolicy() =>
        new AuthorizationPolicyBuilder()
            .RequireClaim("scope", "elwark.people.admin")
            .RequireRole("adm", "ppl.adm")
            .Build();
}
