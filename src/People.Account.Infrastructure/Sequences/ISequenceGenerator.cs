using System.Threading;
using System.Threading.Tasks;
using People.Account.Domain.Aggregates.AccountAggregate;

namespace People.Account.Infrastructure.Sequences
{
    public interface ISequenceGenerator
    {
        Task<AccountId> NextAccountIdAsync(CancellationToken ct = default);
    }
}