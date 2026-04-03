using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using People.IntegrationTests.Infrastructure;
using People.IntegrationTests.Web;
using Xunit;

namespace People.IntegrationTests.Endpoints;

public sealed class AccountMeTests(PostgreSqlFixture postgres) : RestApiTestBase(postgres)
{
    [Fact]
    public async Task GetMe_WithAuth_Returns200()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("me-get@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var response = await client.GetAsync("/accounts/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("id", out var idEl) && idEl.GetInt64() == id);
        Assert.True(doc.RootElement.TryGetProperty("emails", out _));
    }

    [Fact]
    public async Task GetMe_WithoutAuth_Returns401()
    {
        await ResetAsync();
        _ = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("me-anon@example.com"));

        var client = Factory.CreateClient();
        var response = await client.GetAsync("/accounts/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutMe_WithValidBody_Returns200AndUpdatedNickname()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("me-put@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var putBody =
            """
            {"firstName":null,"lastName":null,"nickname":"after-put-nick","preferNickname":true,"language":"en","countryCode":"US","timeZone":"UTC","dateFormat":"yyyy-MM-dd","timeFormat":"HH:mm","startOfWeek":"monday"}
            """;
        var content = new StringContent(putBody.Trim(), Encoding.UTF8, "application/json");
        var put = await client.PutAsync("/accounts/me", content);

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        using var doc = JsonDocument.Parse(await put.Content.ReadAsStringAsync());
        Assert.Equal("after-put-nick", doc.RootElement.GetProperty("nickname").GetString());
    }

    [Fact]
    public async Task PutMe_InvalidNickname_Returns400()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("me-bad@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var putBody =
            """
            {"firstName":null,"lastName":null,"nickname":"ab","preferNickname":true,"language":"en","countryCode":"US","timeZone":"UTC","dateFormat":"yyyy-MM-dd","timeFormat":"HH:mm","startOfWeek":"monday"}
            """;
        var content = new StringContent(putBody.Trim(), Encoding.UTF8, "application/json");
        var put = await client.PutAsync("/accounts/me", content);

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_Returns204AndAccountRemoved()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("me-del@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var del = await client.DeleteAsync("/accounts/me");

        Assert.True(del.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK);

        var getAfter = await client.GetAsync("/accounts/me");
        Assert.Equal(HttpStatusCode.NotFound, getAfter.StatusCode);
    }
}
