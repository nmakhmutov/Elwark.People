using System.Threading;
using System.Threading.Tasks;

namespace Elwark.People.Api.Infrastructure.Services.Facebook
{
    public interface IFacebookApiService
    {
        Task<FacebookAccount> GetAsync(string accessToken, CancellationToken cancellationToken = default);
    }
}