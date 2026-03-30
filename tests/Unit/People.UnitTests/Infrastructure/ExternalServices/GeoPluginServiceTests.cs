using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using People.Api.Infrastructure.Providers;
using Xunit;

namespace People.UnitTests.Infrastructure.ExternalServices;

public sealed class GeoPluginServiceTests
{
    private static IConfiguration Config =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Urls:GeoPlugin.Api"] = "https://geoplugin.test"
            })
            .Build();

    private static GeoPluginService CreateService(MockHttpMessageHandler handler) =>
        new(new HttpClient(handler, disposeHandler: true), Config, NullLogger<GeoPluginService>.Instance);

    [Fact]
    public async Task GetAsync_ValidJson_ReturnsIpInformation()
    {
        var handler = new MockHttpMessageHandler();
        handler.Configure((req, _) =>
        {
            Assert.Contains("ip=192.0.2.10", req.RequestUri?.Query);
            const string json =
                """
                {"geoplugin_city":"Austin",
                 "geoplugin_countryCode":"US",
                 "geoplugin_continentCode":"NA",
                 "geoplugin_timezone":"America/Chicago"}
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var sut = CreateService(handler);
        var info = await sut.GetAsync("192.0.2.10", "en");

        Assert.NotNull(info);
        Assert.Equal("US", info.CountryCode);
        Assert.Equal("NA", info.Region);
        Assert.Equal("Austin", info.City);
        Assert.Equal("America/Chicago", info.TimeZone);
    }

    [Fact]
    public async Task GetAsync_ErrorHttpStatus_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = CreateService(handler);
        Assert.Null(await sut.GetAsync("192.0.2.1", "en"));
    }
}
