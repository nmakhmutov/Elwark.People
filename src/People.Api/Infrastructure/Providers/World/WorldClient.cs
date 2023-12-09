using People.Domain.ValueObjects;

namespace People.Api.Infrastructure.Providers.World;

internal sealed class WorldClient : IWorldClient
{
    private readonly HttpClient _client;

    public WorldClient(HttpClient client) =>
        _client = client;

    public async Task<IReadOnlyList<CountryOverview>> GetCountries(CancellationToken ct = default)
    {
        var response = await _client.GetAsync("/countries", ct);

        if (!response.IsSuccessStatusCode)
            return Array.Empty<CountryOverview>();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<CountryOverview>>(ct) ?? Array.Empty<CountryOverview>();
    }

    public async Task<CountryDetails?> GetCountryAsync(CountryCode code, CancellationToken ct = default)
    {
        if (code.IsEmpty())
            return null;

        var response = await _client.GetAsync($"/countries/{code}", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content
            .ReadFromJsonAsync<CountryDetails>(ct);
    }
}
