using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace People.Infrastructure.Timezones
{
    public interface ITimezoneService
    {
        Task<IReadOnlyCollection<Timezone>> GetAsync(CancellationToken ct = default);
        
        Task<Timezone?> GetAsync(string name, CancellationToken ct = default);
    }
}