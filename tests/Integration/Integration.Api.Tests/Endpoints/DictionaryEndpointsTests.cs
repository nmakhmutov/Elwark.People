using System.Net;
using System.Text.Json;
using Integration.Api.Tests.Web;
using Xunit;

namespace Integration.Api.Tests.Endpoints;

public sealed class DictionaryEndpointsTests(PostgreSqlFixture postgres) : RestApiTestBase(postgres)
{
    [Fact]
    public async Task GetCountries_WithPeopleRead_Returns200AndArray()
    {
        await ResetAsync();
        using var client = Factory.CreateAuthenticatedClient(1, "people:read");

        var response = await client.GetAsync("/countries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.NotEmpty(doc.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task GetTimezones_WithPeopleRead_Returns200AndArray()
    {
        await ResetAsync();
        using var client = Factory.CreateAuthenticatedClient(1, "people:read");

        var response = await client.GetAsync("/timezones");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.NotEmpty(doc.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task GetCountries_WithoutScope_Returns401Or403()
    {
        await ResetAsync();
        using var client = Factory.CreateAuthenticatedClient(1, "people:write");

        var response = await client.GetAsync("/countries");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCountries_Unauthenticated_Returns401Or403()
    {
        await ResetAsync();
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/countries");

        AssertUnauthorizedOrForbidden(response.StatusCode);
    }
}
