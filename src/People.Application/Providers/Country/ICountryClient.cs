using System.Globalization;
using People.Domain.ValueObjects;

namespace People.Application.Providers.Country;

public interface ICountryClient
{
    IAsyncEnumerable<CountryOverview> GetAsync(CultureInfo culture, CancellationToken ct = default);

    Task<CountryDetails?> GetAsync(CountryCode code, CancellationToken ct = default);
}
