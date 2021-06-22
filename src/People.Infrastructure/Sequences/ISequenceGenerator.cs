using System.Threading;
using System.Threading.Tasks;
using People.Domain.Aggregates.Account;

namespace People.Infrastructure.Sequences
{
    public interface ISequenceGenerator
    {
        Task<AccountId> NextAccountIdAsync(CancellationToken ct = default);
    }
}