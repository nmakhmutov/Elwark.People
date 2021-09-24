using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using People.Account.Domain;

namespace People.Account.Infrastructure.Countries
{
    public interface ICountryService
    {
        Task<IReadOnlyCollection<CountrySummary>> GetAsync(Language language, CancellationToken ct = default);
        
        Task<Country?> GetAsync(string code, CancellationToken ct = default);
        
        
    }
}