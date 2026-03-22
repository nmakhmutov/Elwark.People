using Microsoft.EntityFrameworkCore;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;

namespace People.Infrastructure.Repositories;

internal sealed class AccountRepository : IAccountRepository
{
    private readonly PeopleDbContext _dbContext;

    public AccountRepository(PeopleDbContext dbContext) =>
        _dbContext = dbContext;

    public IUnitOfWork UnitOfWork =>
        _dbContext;

    public Task<Account?> GetAsync(AccountId id, CancellationToken ct) =>
        _dbContext.Accounts
            .Include(x => x.Emails)
            .Include(x => x.Externals)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Account> AddAsync(Account account, CancellationToken ct) =>
        (await _dbContext.Accounts.AddAsync(account, ct)).Entity;

    public void Delete(Account entity) =>
        _dbContext.Accounts.Remove(entity);
}
