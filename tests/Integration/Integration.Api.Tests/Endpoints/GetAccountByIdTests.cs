using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Integration.Shared.Tests.Infrastructure;
using Integration.Api.Tests.Web;
using Xunit;

namespace Integration.Api.Tests.Endpoints;

public sealed class GetAccountByIdTests(PostgreSqlFixture postgres) : RestApiTestBase(postgres)
{
    [Fact]
    public async Task GetAccountById_WithPeopleReadScope_Returns200AndAccountSummaryShape()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("get-by-id@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:read");
        var response = await client.GetAsync($"/accounts/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("id", out var idEl) && idEl.GetInt64() == id);
        Assert.True(root.TryGetProperty("email", out var emailEl) && emailEl.GetString() == "get-by-id@example.com");
        Assert.True(root.TryGetProperty("nickname", out _));
        Assert.True(!root.TryGetProperty("firstName", out var fn) || fn.ValueKind is JsonValueKind.String or JsonValueKind.Null);
        Assert.True(!root.TryGetProperty("lastName", out var ln) || ln.ValueKind is JsonValueKind.String or JsonValueKind.Null);
        Assert.True(root.TryGetProperty("fullName", out _));
        Assert.True(root.TryGetProperty("language", out _));
        Assert.True(root.TryGetProperty("picture", out _));
        Assert.True(!root.TryGetProperty("regionCode", out var rc) || rc.ValueKind is JsonValueKind.String or JsonValueKind.Null);
        Assert.True(!root.TryGetProperty("countryCode", out var cc) || cc.ValueKind is JsonValueKind.String or JsonValueKind.Null);
        Assert.True(root.TryGetProperty("timeZone", out _));
        Assert.True(root.TryGetProperty("dateFormat", out _));
        Assert.True(root.TryGetProperty("timeFormat", out _));
        Assert.True(root.TryGetProperty("startOfWeek", out _));
        Assert.True(!root.TryGetProperty("isBanned", out var banned) || banned.ValueKind == JsonValueKind.False);
    }

    [Fact]
    public async Task GetAccountById_WithoutBearer_Returns401Or403()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("anon@example.com"));

        var client = Factory.CreateClient();
        var response = await client.GetAsync($"/accounts/{id}");

        AssertUnauthorizedOrForbidden(response.StatusCode);
    }

    [Fact]
    public async Task GetAccountById_WithTokenMissingReadScope_Returns403()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("no-read@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var response = await client.GetAsync($"/accounts/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountById_UnknownId_Returns404()
    {
        await ResetAsync();
        _ = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("ctx@example.com"));

        using var client = Factory.CreateAuthenticatedClient(1, "people:read");
        var response = await client.GetAsync("/accounts/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
