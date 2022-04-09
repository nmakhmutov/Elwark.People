using System.Threading;
using System.Threading.Tasks;

namespace People.Infrastructure.Blacklist;

public interface IBlacklistService
{
    Task<bool> IsPasswordForbidden(string password, CancellationToken ct = default);

    Task<bool> IsEmailHostDenied(string host, CancellationToken ct = default);
}