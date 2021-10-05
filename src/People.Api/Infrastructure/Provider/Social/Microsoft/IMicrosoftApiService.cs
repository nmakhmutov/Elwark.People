using System.Threading;
using System.Threading.Tasks;

namespace People.Api.Infrastructure.Provider.Social.Microsoft;

public interface IMicrosoftApiService
{
    Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken ct = default);
}
