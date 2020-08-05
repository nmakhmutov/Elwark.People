using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.SeedWork;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public interface IAccountRepository : IRepository
    {
        Task<Account> GetAsync(AccountId id, CancellationToken cancellationToken = default);

        Task<Account> GetAsync(IdentityId id, CancellationToken cancellationToken = default);

        Task<Account> GetAsync(Identification identification, CancellationToken cancellationToken = default);

        Task<Account> CreateAsync(Account account, CancellationToken cancellationToken = default);

        Account Update(Account account);

        Identity Update(Identity identity);
    }
}