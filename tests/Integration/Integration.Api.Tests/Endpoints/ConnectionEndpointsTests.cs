using System.Net;
using System.Net.Mail;
using Integration.Shared.Tests.Infrastructure;
using Integration.Api.Tests.Web;
using Xunit;

namespace Integration.Api.Tests.Endpoints;

public sealed class ConnectionEndpointsTests(PostgreSqlFixture postgres) : RestApiTestBase(postgres)
{
    [Fact]
    public async Task DeleteGoogleIdentity_Existing_Returns204()
    {
        await ResetAsync();
        var id = await SeedAccountWithGoogleConnectionAsync(
            Factory,
            new MailAddress("g-del@example.com"),
            "google-identity-1");

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var response = await client.DeleteAsync("/accounts/me/connections/google/identities/google-identity-1");

        Assert.True(response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteMicrosoftIdentity_Existing_Returns204()
    {
        await ResetAsync();
        var id = await SeedAccountWithMicrosoftConnectionAsync(
            Factory,
            new MailAddress("ms-del@example.com"),
            "ms-identity-1");

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var response = await client.DeleteAsync("/accounts/me/connections/microsoft/identities/ms-identity-1");

        Assert.True(response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteGoogleIdentity_NotLinked_Returns204_Idempotent()
    {
        await ResetAsync();
        var id = await SeedAccountWithGoogleConnectionAsync(
            Factory,
            new MailAddress("g-none@example.com"),
            "google-real");

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var response = await client.DeleteAsync("/accounts/me/connections/google/identities/nonexistent-subject");

        Assert.True(response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK);
    }
}
