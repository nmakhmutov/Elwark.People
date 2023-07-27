using People.Domain.ValueObjects;

namespace People.Api.Infrastructure.Providers.World;

internal interface IWorldClient
{
    public Task<IReadOnlyList<CountryOverview>> GetCountries(CancellationToken ct = default);

    public Task<CountryDetails?> GetCountryAsync(CountryCode code, CancellationToken ct = default);
}
