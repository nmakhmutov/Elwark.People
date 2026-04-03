using System.Net;
using System.Net.Http.Json;
using Integration.Api.Tests.Web;
using Microsoft.EntityFrameworkCore;
using People.Application.Webhooks;
using Xunit;

namespace Integration.Api.Tests.Endpoints;

public sealed class WebhookEndpointsTests(PostgreSqlFixture postgres) : RestApiTestBase(postgres)
{
    [Fact]
    public async Task Crud_RoundTrip_PersistsWebhookConsumer()
    {
        await ResetAsync();

        using var client = Factory.CreateAdminClient(1);

        var create = await client.PostAsJsonAsync("/webhooks", new
        {
            type = WebhookType.Created,
            method = WebhookMethod.Post,
            destinationUrl = "https://hooks.example/create",
            token = "token-1"
        });

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<WebhookDto>();
        Assert.NotNull(created);

        var get = await client.GetAsync($"/webhooks/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var update = await client.PutAsJsonAsync($"/webhooks/{created.Id}", new
        {
            type = WebhookType.Updated,
            method = WebhookMethod.Put,
            destinationUrl = "https://hooks.example/update",
            token = "token-2"
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var list = await client.GetFromJsonAsync<List<WebhookDto>>("/webhooks");
        var listed = Assert.Single(list!);
        Assert.Equal(WebhookType.Updated.ToString(), listed.Type);
        Assert.Equal(WebhookMethod.Put.ToString(), listed.Method);
        Assert.Equal("https://hooks.example/update", listed.DestinationUrl);
        Assert.Equal("token-2", listed.Token);

        var delete = await client.DeleteAsync($"/webhooks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        await using var db = Postgres.CreateWebhookContext();
        Assert.Equal(0, await db.Consumers.CountAsync());
    }

    [Fact]
    public async Task AdminEndpoint_WithoutAdminScopeAndRole_IsForbidden()
    {
        await ResetAsync();

        using var client = Factory.CreateAuthenticatedClient(1, "people:read");
        var response = await client.GetAsync("/webhooks");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record WebhookDto(Guid Id, string Type, string Method, string DestinationUrl, string? Token);
}
