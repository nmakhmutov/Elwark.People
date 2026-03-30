using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using People.Api.Infrastructure.Providers;
using Xunit;

namespace People.UnitTests.Infrastructure.ExternalServices;

public sealed class IpQueryServiceTests
{
    private static IConfiguration Config =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Urls:IpQuery.Api"] = "https://ipquery.test/lookup"
            })
            .Build();

    private static IpQueryService CreateService(MockHttpMessageHandler handler) =>
        new(new HttpClient(handler, disposeHandler: true), Config, NullLogger<IpQueryService>.Instance);

    [Fact]
    public async Task GetAsync_ValidJson_ReturnsIpInformation()
    {
        var handler = new MockHttpMessageHandler();
        handler.Configure((req, _) =>
        {
            var url = req.RequestUri?.ToString();
            Assert.Equal("https://ipquery.test/lookup/198.51.100.2?format=json", url);
            const string json =
                """
                {"location":{"country_code":"FR","city":"Paris","timezone":"Europe/Paris"}}
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var sut = CreateService(handler);
        var info = await sut.GetAsync("198.51.100.2", "en");

        Assert.NotNull(info);
        Assert.Equal("FR", info.CountryCode);
        Assert.Null(info.Region);
        Assert.Equal("Paris", info.City);
        Assert.Equal("Europe/Paris", info.TimeZone);
    }

    [Fact]
    public async Task GetAsync_ErrorHttpStatus_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var sut = CreateService(handler);
        Assert.Null(await sut.GetAsync("9.9.9.9", "en"));
    }

    [Fact]
    public async Task GetAsync_MalformedJson_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{", Encoding.UTF8, "application/json")
        });

        var sut = CreateService(handler);
        Assert.Null(await sut.GetAsync("9.9.9.9", "en"));
    }
}
