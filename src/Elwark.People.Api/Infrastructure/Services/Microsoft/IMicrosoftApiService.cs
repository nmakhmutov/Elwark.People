using System.Threading;
using System.Threading.Tasks;

namespace Elwark.People.Api.Infrastructure.Services.Microsoft
{
    public interface IMicrosoftApiService
    {
        Task<MicrosoftAccount> GetAsync(string accessToken, CancellationToken cancellationToken = default);
    }
}