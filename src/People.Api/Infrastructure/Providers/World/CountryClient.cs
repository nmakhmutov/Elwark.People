using People.Domain.ValueObjects;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Hybrid;

namespace People.Api.Infrastructure.Providers.World;

internal sealed class CountryClient : ICountryClient
{
    private readonly HttpClient _client;
    private readonly HybridCache _cache;

    public CountryClient(HttpClient client, HybridCache cache)
    {
        _client = client;
        _cache = cache;
    }

    public async IAsyncEnumerable<CountryOverview> GetAsync(
        CultureInfo culture,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var countries = await _cache.GetOrCreateAsync(
            $"countries-{culture.TwoLetterISOLanguageName}", async token =>
            {
                var response = await _client.GetAsync("all?fields=name,cca2,cca3,region,subregion,translations", token);

                if (!response.IsSuccessStatusCode)
                    return [];

                return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<RestCountry>>(token) ?? [];
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromHours(1),
                LocalCacheExpiration = TimeSpan.FromHours(1),
            },
            ["countries"],
            ct
        );

        var result = countries.Select(x => new CountryOverview(
            x.Cca2 ?? string.Empty,
            x.Cca3 ?? string.Empty,
            MapRegion(x.Region, x.Subregion),
            GetCountryName(x, culture.ThreeLetterISOLanguageName))
        );

        foreach (var country in result.OrderBy(x => x.Name))
            yield return country;
    }

    public async Task<CountryDetails?> GetAsync(CountryCode code, CancellationToken ct = default)
    {
        if (code.IsEmpty())
            return null;

        var response = await _client.GetAsync($"alpha/{code}?fields=cca2,cca3,region,subregion,ccn3,translations", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        var country = await response.Content.ReadFromJsonAsync<RestCountry>(ct);

        if (country is null)
            return null;

        return new CountryDetails(
            country.Ccn3 ?? string.Empty,
            country.Cca2 ?? string.Empty,
            country.Cca3 ?? string.Empty,
            MapRegion(country.Region, country.Subregion)
        );
    }

    private static string GetCountryName(RestCountry country, string language)
    {
        if (country.Translations is not null && country.Translations.TryGetValue(language, out var translation))
            return translation.Common ?? country.Name?.Common ?? string.Empty;

        return country.Name?.Common ?? string.Empty;
    }

    private static RegionCode MapRegion(string? region, string? subregion)
    {
        if (string.IsNullOrWhiteSpace(region))
            return RegionCode.Empty;

        return region.ToLowerInvariant() switch
        {
            "africa" => RegionCode.Parse("AF"),
            "antarctic" => RegionCode.Parse("AN"),
            "asia" => RegionCode.Parse("AS"),
            "europe" => RegionCode.Parse("EU"),
            "oceania" => RegionCode.Parse("OC"),
            "americas" => subregion?.ToLowerInvariant() switch
            {
                "south america" => RegionCode.Parse("SA"),
                _ => RegionCode.Parse("NA")
            },
            _ => RegionCode.Empty
        };
    }

    private sealed record RestCountry(
        [property: JsonPropertyName("name")] RestCountryName? Name,
        [property: JsonPropertyName("cca2")] string? Cca2,
        [property: JsonPropertyName("cca3")] string? Cca3,
        [property: JsonPropertyName("region")] string? Region,
        [property: JsonPropertyName("subregion")] string? Subregion,
        [property: JsonPropertyName("ccn3")] string? Ccn3,
        [property: JsonPropertyName("translations")] IReadOnlyDictionary<string, RestCountryTranslation>? Translations
    );

    private sealed record RestCountryName([property: JsonPropertyName("common")] string? Common);

    private sealed record RestCountryTranslation([property: JsonPropertyName("common")] string? Common);
}
