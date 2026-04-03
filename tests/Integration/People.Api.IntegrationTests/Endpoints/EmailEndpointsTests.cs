using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using NSubstitute;
using People.Domain.ValueObjects;
using People.IntegrationTests.Infrastructure;
using People.IntegrationTests.Web;
using Xunit;

namespace People.IntegrationTests.Endpoints;

public sealed class EmailEndpointsTests(PostgreSqlFixture postgres) : RestApiTestBase(postgres)
{
    private static readonly JsonSerializerOptions JsonWrite = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task PostAppendEmail_Valid_Returns200()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("append-base@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var content = new StringContent(
            """{"email":"append-extra@example.com"}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/accounts/me/emails", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("append-extra@example.com", body, StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (root.TryGetProperty("isConfirmed", out var conf) || root.TryGetProperty("IsConfirmed", out conf))
            Assert.False(conf.GetBoolean());
    }

    [Fact]
    public async Task PostAppendEmail_Invalid_Returns400()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("append-inv@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var content = new StringContent(
            """{"email":"not-an-email"}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/accounts/me/emails", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSecondaryEmail_Returns204()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("del-base@example.com"));

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");
        var post = new StringContent(
            """{"email":"del-secondary@example.com"}""",
            Encoding.UTF8,
            "application/json");
        (await client.PostAsync("/accounts/me/emails", post)).EnsureSuccessStatusCode();

        var del = await client.DeleteAsync($"/accounts/me/emails/{Uri.EscapeDataString("del-secondary@example.com")}");

        Assert.True(del.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostEmailStatus_ChangesPrimary_WhenSecondaryConfirmed()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("primary-old@example.com"));

        string? capturedCode = null;
        Factory.Notification
            .SendConfirmationAsync(
                Arg.Any<MailAddress>(),
                Arg.Do<string>(c => capturedCode = c),
                Arg.Any<Language>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");

        var append = new StringContent(
            """{"email":"primary-new@example.com"}""",
            Encoding.UTF8,
            "application/json");
        (await client.PostAsync("/accounts/me/emails", append)).EnsureSuccessStatusCode();

        var confirming = new StringContent(
            """{"email":"primary-new@example.com"}""",
            Encoding.UTF8,
            "application/json");
        var postVerify = await client.PostAsync("/accounts/me/emails/verify", confirming);
        postVerify.EnsureSuccessStatusCode();
        using (var tokenDoc = JsonDocument.Parse(await postVerify.Content.ReadAsStringAsync()))
        {
            var token = tokenDoc.RootElement.GetProperty("token").GetString();
            Assert.NotNull(capturedCode);

            var confirmBody = new StringContent(
                JsonSerializer.Serialize(new { token, code = capturedCode }, JsonWrite),
                Encoding.UTF8,
                "application/json");
            (await client.PutAsync("/accounts/me/emails/verify", confirmBody)).EnsureSuccessStatusCode();
        }

        var statusBody = new StringContent(
            """{"email":"primary-new@example.com"}""",
            Encoding.UTF8,
            "application/json");
        var status = await client.PostAsync("/accounts/me/emails/status", statusBody);
        status.EnsureSuccessStatusCode();

        using var listDoc = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, listDoc.RootElement.ValueKind);
        JsonElement? primaryRow = null;
        foreach (var e in listDoc.RootElement.EnumerateArray())
        {
            if (e.TryGetProperty("isPrimary", out var ip) && ip.GetBoolean())
            {
                primaryRow = e;
                break;
            }
        }

        Assert.True(primaryRow is not null);
        Assert.True(
            primaryRow.Value.TryGetProperty("value", out var addr) || primaryRow.Value.TryGetProperty("Value", out addr));
        Assert.Equal("primary-new@example.com", addr.GetString());
    }

    [Fact]
    public async Task EmailVerifyFlow_PostThenPut_ConfirmsSecondary()
    {
        await ResetAsync();
        var id = await SeedAccountWithConfirmedPrimaryAsync(Factory, new MailAddress("verify-base@example.com"));

        string? capturedCode = null;
        Factory.Notification
            .SendConfirmationAsync(
                Arg.Any<MailAddress>(),
                Arg.Do<string>(c => capturedCode = c),
                Arg.Any<Language>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        using var client = Factory.CreateAuthenticatedClient(id, "people:write");

        var append = new StringContent(
            """{"email":"verify-target@example.com"}""",
            Encoding.UTF8,
            "application/json");
        (await client.PostAsync("/accounts/me/emails", append)).EnsureSuccessStatusCode();

        var confirming = new StringContent(
            """{"email":"verify-target@example.com"}""",
            Encoding.UTF8,
            "application/json");
        var postRes = await client.PostAsync("/accounts/me/emails/verify", confirming);
        postRes.EnsureSuccessStatusCode();

        using var postDoc = JsonDocument.Parse(await postRes.Content.ReadAsStringAsync());
        var token = postDoc.RootElement.GetProperty("token").GetString();
        Assert.NotNull(capturedCode);

        var putBody = new StringContent(
            JsonSerializer.Serialize(new { token, code = capturedCode }, JsonWrite),
            Encoding.UTF8,
            "application/json");
        var putRes = await client.PutAsync("/accounts/me/emails/verify", putBody);
        putRes.EnsureSuccessStatusCode();

        using var emailDoc = JsonDocument.Parse(await putRes.Content.ReadAsStringAsync());
        Assert.True(
            emailDoc.RootElement.TryGetProperty("value", out var ev) ||
            emailDoc.RootElement.TryGetProperty("Value", out ev));
        Assert.Equal("verify-target@example.com", ev.GetString());
        Assert.True(
            emailDoc.RootElement.TryGetProperty("isConfirmed", out var ic) ||
            emailDoc.RootElement.TryGetProperty("IsConfirmed", out ic));
        Assert.True(ic.GetBoolean());
    }
}
