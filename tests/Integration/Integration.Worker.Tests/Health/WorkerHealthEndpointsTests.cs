using System.Net;
using Integration.Shared.Tests.Infrastructure;
using Xunit;

namespace Integration.Worker.Tests.Health;

[Collection(nameof(PostgresCollection))]
public sealed class WorkerHealthEndpointsTests(PostgreSqlFixture postgres) : IAsyncLifetime
{
    private const string InvalidConnectionString = "Host=127.0.0.1;Port=1;Database=missing;Username=missing;Password=missing";
    private readonly WorkerHostFactory _factory = new(postgres);

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Get_ReturnsOk_ForHealthEndpoint(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task GetReady_ReturnsOk_WhenDatabaseConnectionIsInvalid()
    {
        await using var factory = new WorkerHostFactory(postgres, InvalidConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
