using System.Threading;
using System.Threading.Tasks;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Seed;

namespace People.Domain.Aggregates.AccountAggregate;

public interface IAccountRepository : IRepository<Account, AccountId>
{
    public Task<Account?> GetAsync(Identity key, CancellationToken ct = default);

    public Task<bool> IsExists(Identity key, CancellationToken ct = default);
}
