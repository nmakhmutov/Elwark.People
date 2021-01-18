using System.Threading;
using System.Threading.Tasks;

namespace People.Api.Infrastructure.Password
{
    public interface IPasswordValidator
    {
        Task ValidateAsync(string password, CancellationToken ct = default);
    }
}