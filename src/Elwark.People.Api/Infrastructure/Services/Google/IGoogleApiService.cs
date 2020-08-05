using System.Threading;
using System.Threading.Tasks;

namespace Elwark.People.Api.Infrastructure.Services.Google
{
    public interface IGoogleApiService
    {
        Task<GoogleAccount> GetAsync(string accessToken, CancellationToken cancellationToken = default);
    }
}