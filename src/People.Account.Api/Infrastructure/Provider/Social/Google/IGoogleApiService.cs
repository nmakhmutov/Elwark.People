using System.Threading;
using System.Threading.Tasks;

namespace People.Account.Api.Infrastructure.Provider.Social.Google
{
    public interface IGoogleApiService
    {
        Task<GoogleAccount> GetAsync(string accessToken, CancellationToken ct = default);
    }
}