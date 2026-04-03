using System.Net;
using System.Net.Mail;
using Integration.Api.Tests.Web;
using Xunit;

namespace Integration.Api.Tests.Endpoints;

/// <summary>
/// Policy names map to scopes: <see cref="People.Api.Infrastructure.Policy.RequireRead"/> → <c>people:read</c>,
/// <see cref="People.Api.Infrastructure.Policy.RequireWrite"/> → authenticated + <c>people:write</c>.
/// </summary>
public sealed class AuthorizationPolicyTests(PostgreSqlFixture postgres) : RestApiTestBase(postgres)
{
    [Fact]
    public async Task RequireRead_Endpoints_RejectTokenWithoutPeopleRead()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("pol-read@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var response = await client.GetAsync($"/accounts/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RequireWrite_Endpoints_RejectTokenWithoutPeopleWrite()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("pol-write@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:read");
        var response = await client.GetAsync("/accounts/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ValidJwt_WithSubOnly_NoScopes_IsForbiddenForReadAndWrite()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("pol-bare@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id);
        var getById = await client.GetAsync($"/accounts/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, getById.StatusCode);

        var me = await client.GetAsync("/accounts/me");
        Assert.Equal(HttpStatusCode.Forbidden, me.StatusCode);
    }
}
