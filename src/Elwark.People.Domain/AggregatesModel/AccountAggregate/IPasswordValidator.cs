using System.Threading;
using System.Threading.Tasks;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public interface IPasswordValidator
    {
        Task ValidateAsync(string password, CancellationToken ct = default);
    }
}