using System.Threading;
using System.Threading.Tasks;

namespace People.Infrastructure.Timezones
{
    public interface ITimezoneService
    {
        Task<Timezone?> GetAsync(string name, CancellationToken ct = default);
    }
}