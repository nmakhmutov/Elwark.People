using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using People.Infrastructure.Providers.Ip;
using Xunit;

namespace People.UnitTests.Infrastructure.ExternalServices;

public sealed class IpApiServiceTests
{
    private static IConfiguration Config =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Urls:Ip.Api"] = "https://ipapi.test"
            })
            .Build();

    private static IpApiService CreateService(MockHttpMessageHandler handler) =>
        new(new HttpClient(handler, disposeHandler: true), Config, NullLogger<IpApiService>.Instance);

    [Fact]
    public async Task GetAsync_StatusSuccess_ReturnsIpInformation()
    {
        var handler = new MockHttpMessageHandler();
        handler.Configure((req, _) =>
        {
            var url = req.RequestUri?.ToString();
            var query = req.RequestUri?.Query;
            Assert.Contains("/json/203.0.113.5", url);
            Assert.Contains("lang=de", query);
            const string json =
                """
                {"status":"success",
                "countryCode":"DE",
                "continentCode":"EU",
                "city":"Berlin",
                "timeZone":"Europe/Berlin"}
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var sut = CreateService(handler);
        var info = await sut.GetAsync("203.0.113.5", "de");

        Assert.NotNull(info);
        Assert.Equal("DE", info.CountryCode);
        Assert.Equal("EU", info.Region);
        Assert.Equal("Berlin", info.City);
        Assert.Equal("Europe/Berlin", info.TimeZone);
    }

    [Fact]
    public async Task GetAsync_StatusFail_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"status":"fail","countryCode":"","continentCode":"","city":null,"timeZone":""}""",
                Encoding.UTF8,
                "application/json")
        });

        var sut = CreateService(handler);
        var info = await sut.GetAsync("0.0.0.0", "en");

        Assert.Null(info);
    }

    [Fact]
    public async Task GetAsync_StatusFailCaseInsensitive_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"status":"Fail","countryCode":"US","continentCode":"NA","city":"X","timeZone":"tz"}""",
                Encoding.UTF8,
                "application/json")
        });

        var sut = CreateService(handler);
        Assert.Null(await sut.GetAsync("1.1.1.1", "en"));
    }

    [Fact]
    public async Task GetAsync_ServerError_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.BadGateway));

        var sut = CreateService(handler);
        Assert.Null(await sut.GetAsync("8.8.8.8", "en"));
    }

    [Fact]
    public async Task GetAsync_MalformedJson_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json", Encoding.UTF8, "application/json")
        });

        var sut = CreateService(handler);
        Assert.Null(await sut.GetAsync("8.8.8.8", "en"));
    }
}
