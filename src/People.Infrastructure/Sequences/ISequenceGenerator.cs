using System.Threading;
using System.Threading.Tasks;
using People.Domain.AggregateModels.Account;

namespace People.Infrastructure.Sequences
{
    public interface ISequenceGenerator
    {
        Task<AccountId> NextAccountIdAsync(CancellationToken ct = default);
    }
}