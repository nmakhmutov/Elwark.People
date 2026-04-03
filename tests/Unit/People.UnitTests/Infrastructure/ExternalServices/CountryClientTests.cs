using System.Globalization;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using People.Application.Providers.Country;
using People.Infrastructure.Providers;
using People.Domain.ValueObjects;
using Xunit;

namespace People.UnitTests.Infrastructure.ExternalServices;

/// <remarks>
/// <see cref="HybridCache"/> is sealed, so caching is exercised with a real in-memory instance from
/// <c>AddHybridCache()</c> while HTTP is still fully mocked.
/// </remarks>
public sealed class CountryClientTests
{
    private static HybridCache CreateHybridCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<HybridCache>();
    }

    private static async Task<List<CountryOverview>> ToListAsync(
        IAsyncEnumerable<CountryOverview> source,
        CancellationToken ct = default)
    {
        var list = new List<CountryOverview>();
        await foreach (var item in source.WithCancellation(ct))
            list.Add(item);
        return list;
    }

    [Fact]
    public async Task GetAsync_ValidJson_ReturnsCountries()
    {
        var json = """
                   [{"name":{"common":"Germany"},"cca2":"DE","cca3":"DEU","region":"Europe",
                     "subregion":"Western Europe","translations":{"eng":{"common":"Germany"}}}]
                   """;

        var handler = new MockHttpMessageHandler();
        handler.Configure((req, _) =>
        {
            var path = req.RequestUri?.PathAndQuery.Split('?')[0];
            Assert.Equal("/all", path);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        });

        var http = new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://countries.test/") };
        var sut = new CountryClient(http, CreateHybridCache());

        var list = await ToListAsync(sut.GetAsync(CultureInfo.GetCultureInfo("en"), CancellationToken.None));

        Assert.Single(list);
        var de = list[0];
        Assert.Equal("DE", de.Alpha2);
        Assert.Equal("DEU", de.Alpha3);
        Assert.Equal(RegionCode.Parse("EU"), de.Region);
        Assert.Equal("Germany", de.Name);
    }

    [Fact]
    public async Task GetAsync_SecondCallWithSameCulture_UsesCache_OneHttpRequest()
    {
        var json = """[{"name":{"common":"Zedland"},"cca2":"ZZ","cca3":"ZZZ","region":"Europe","translations":{}}]""";

        var handler = new MockHttpMessageHandler();
        handler.Configure((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        }));

        var http = new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://countries.test/") };
        var sut = new CountryClient(http, CreateHybridCache());
        var culture = CultureInfo.GetCultureInfo("en");

        _ = await ToListAsync(sut.GetAsync(culture, CancellationToken.None));
        _ = await ToListAsync(sut.GetAsync(culture, CancellationToken.None));

        Assert.Equal(1, handler.SendCallCount);
    }

    [Fact]
    public async Task GetAsync_ApiError_ReturnsEmpty_AndDoesNotThrow()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));

        var http = new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://countries.test/") };
        var sut = new CountryClient(http, CreateHybridCache());

        var list = await ToListAsync(sut.GetAsync(CultureInfo.InvariantCulture, CancellationToken.None));

        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAsync_CountryCode_MapsEuropeToEu()
    {
        const string json = """{"cca2":"FR","cca3":"FRA","region":"Europe","subregion":"Western Europe","ccn3":"250"}""";

        var handler = new MockHttpMessageHandler();
        handler.Configure((req, _) =>
        {
            var pathAndQuery = req.RequestUri?.PathAndQuery ?? "";
            Assert.StartsWith("/alpha/FR", pathAndQuery);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        });

        var http = new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://countries.test/") };
        var sut = new CountryClient(http, CreateHybridCache());

        var details = await sut.GetAsync(CountryCode.Parse("FR"), CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(RegionCode.Parse("EU"), details.Region);
    }

    [Fact]
    public async Task GetAsync_CountryCode_MapsAmericasNorthAmericaToNa()
    {
        var json = """{"cca2":"US","cca3":"USA","region":"Americas","subregion":"North America","ccn3":"840"}""";

        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var http = new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://countries.test/") };
        var sut = new CountryClient(http, CreateHybridCache());

        var details = await sut.GetAsync(CountryCode.Parse("US"), CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(RegionCode.Parse("NA"), details.Region);
    }

    [Fact]
    public async Task GetAsync_CountryCode_MapsSouthAmericaSubregionToSa()
    {
        var json = """{"cca2":"BR","cca3":"BRA","region":"Americas","subregion":"South America","ccn3":"076"}""";

        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var http = new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://countries.test/") };
        var sut = new CountryClient(http, CreateHybridCache());

        var details = await sut.GetAsync(CountryCode.Parse("BR"), CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(RegionCode.Parse("SA"), details.Region);
    }
}
