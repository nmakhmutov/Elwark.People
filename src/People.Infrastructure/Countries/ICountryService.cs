using System.Threading;
using System.Threading.Tasks;

namespace People.Infrastructure.Countries
{
    public interface ICountryService
    {
        Task<Country?> GetAsync(string code, CancellationToken ct = default);
    }
}