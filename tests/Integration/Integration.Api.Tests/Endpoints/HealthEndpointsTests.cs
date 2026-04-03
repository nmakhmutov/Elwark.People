using System.Net;
using System.Text.Json;
using Integration.Api.Tests.Web;
using Xunit;

namespace Integration.Api.Tests.Endpoints;

public sealed class HealthEndpointsTests(PostgreSqlFixture postgres) : RestApiTestBase(postgres)
{
    [Fact]
    public async Task GetLive_ReturnsOk()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReady_ReturnsOkWithDatabaseEntries()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entries = document.RootElement.GetProperty("entries");

        Assert.True(entries.TryGetProperty("people-db", out _));
        Assert.True(entries.TryGetProperty("webhook-db", out _));
    }
}
