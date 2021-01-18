using System.Threading;
using System.Threading.Tasks;

namespace People.Api.Infrastructure.Providers.Microsoft
{
    public interface IMicrosoftApiService
    {
        Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken ct = default);
    }
}