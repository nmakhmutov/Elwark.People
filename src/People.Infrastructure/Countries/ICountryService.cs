using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Mongo;
using People.Domain;

namespace People.Infrastructure.Countries;

public interface ICountryService
{
    Task<IReadOnlyCollection<CountrySummary>> GetAsync(Language language, CancellationToken ct = default);

    Task<MongoPagingResult<CountrySummary>> GetAsync(string? code, int page, int limit, CancellationToken ct = default);

    Task<Country?> GetAsync(string code, CancellationToken ct = default);

    Task<Country> CreateAsync(Country country, CancellationToken ct = default);

    Task<Country> UpdateAsync(Country country, CancellationToken ct = default);

    Task DeleteAsync(string code, CancellationToken ct = default);
}
