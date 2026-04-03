using Microsoft.AspNetCore.Authorization.Infrastructure;
using People.Api.Infrastructure;
using Xunit;

namespace Unit.Api.Tests.Infrastructure;

public sealed class PolicyTests
{
    [Fact]
    public void RequireRead_RequiresScopePeopleRead()
    {
        var policy = Policy.RequireRead.Policy;

        var scope = Assert.Single(policy.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.Equal("scope", scope.ClaimType);
        Assert.Equal(["people:read"], scope.AllowedValues);
    }

    [Fact]
    public void RequireWrite_RequiresAuthenticatedUserAndScopePeopleWrite()
    {
        var policy = Policy.RequireWrite.Policy;

        Assert.Single(policy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>());

        var scope = Assert.Single(policy.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.Equal("scope", scope.ClaimType);
        Assert.Equal(["people:write"], scope.AllowedValues);
    }

    [Fact]
    public void RequireAdmin_RequiresScopePeopleAdminAndRoles()
    {
        var policy = Policy.RequireAdmin.Policy;

        var scope = Assert.Single(policy.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.Equal("scope", scope.ClaimType);
        Assert.Equal(["people:admin"], scope.AllowedValues);

        var roles = Assert.Single(policy.Requirements.OfType<RolesAuthorizationRequirement>());
        Assert.Equal(2, roles.AllowedRoles.Count());
        Assert.Contains("adm", roles.AllowedRoles);
        Assert.Contains("ppl.adm", roles.AllowedRoles);
    }
}
