using System.Threading;
using System.Threading.Tasks;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Account.Domain.Seed;

namespace People.Account.Domain.Aggregates.AccountAggregate
{
    public interface IAccountRepository : IRepository<Account, AccountId>
    {
        public Task<Account?> GetAsync(Identity key, CancellationToken ct = default);

        public Task<bool> IsExists(Identity key, CancellationToken ct = default);
    }
}
