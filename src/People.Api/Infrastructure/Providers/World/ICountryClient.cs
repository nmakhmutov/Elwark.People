using System.Globalization;
using People.Domain.ValueObjects;

namespace People.Api.Infrastructure.Providers.World;

internal interface ICountryClient
{
    IAsyncEnumerable<CountryOverview> GetAsync(CultureInfo culture, CancellationToken ct = default);

    Task<CountryDetails?> GetAsync(CountryCode code, CancellationToken ct = default);
}
