using System.Threading;
using System.Threading.Tasks;

namespace People.Account.Infrastructure.Forbidden
{
    public interface IForbiddenService
    {
        Task<bool> IsPasswordForbidden(string password, CancellationToken ct = default);
        
        Task<bool> IsEmailHostDenied(string host, CancellationToken ct = default);
    }
}