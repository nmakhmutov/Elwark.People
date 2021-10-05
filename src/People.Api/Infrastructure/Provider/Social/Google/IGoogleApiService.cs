using System.Threading;
using System.Threading.Tasks;

namespace People.Api.Infrastructure.Provider.Social.Google;

public interface IGoogleApiService
{
    Task<GoogleAccount> GetAsync(string accessToken, CancellationToken ct = default);
}
