using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace Elwark.People.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly OAuthContext _context;

        public AccountRepository(OAuthContext context) => _context = context;

        public IUnitOfWork UnitOfWork =>
            _context;

        public async Task<Account> CreateAsync(Account account, CancellationToken cancellationToken) =>
            account.IsTransient()
                ? (await _context.Accounts.AddAsync(account, cancellationToken)).Entity
                : account;

        public Account Update(Account account) =>
            _context.Accounts.Update(account).Entity;

        public Identity Update(Identity identity) =>
            _context.Identities.Update(identity).Entity;

        public Task<Account> GetAsync(AccountId id, CancellationToken cancellationToken) =>
            _context.Accounts
                .Include(x => x.Ban)
                .Include(x => x.Password)
                .Include(x => x.Identities)
                .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken);

        public Task<Account> GetAsync(Identification identification, CancellationToken cancellationToken) =>
            _context.Accounts
                .Include(x => x.Ban)
                .Include(x => x.Password)
                .Include(x => x.Identities)
                .FirstOrDefaultAsync(x => x.Identities.Any(t =>
                        t.Identification.Type == identification.Type &&
                        t.Identification.Value == identification.Value),
                    cancellationToken);

        public Task<Account> GetAsync(IdentityId id, CancellationToken cancellationToken) =>
            _context.Accounts
                .Include(x => x.Ban)
                .Include(x => x.Password)
                .Include(x => x.Identities)
                .FirstOrDefaultAsync(x => x.Identities.Any(t => t.Id == id.Value), cancellationToken);
    }
}