using Microsoft.EntityFrameworkCore;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.SeedWork;

namespace People.Infrastructure.Repositories;

internal sealed class AccountRepository : IAccountRepository
{
    private readonly PeopleDbContext _dbContext;

    public AccountRepository(PeopleDbContext dbContext) =>
        _dbContext = dbContext;

    public IUnitOfWork UnitOfWork =>
        _dbContext;

    public async Task<Account?> GetAsync(long id, CancellationToken ct)
    {
        var account = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (account is null)
            return null;
        
        await _dbContext.Entry(account).Collection(x => x.Emails).LoadAsync(ct);
        await _dbContext.Entry(account).Collection(x => x.Externals).LoadAsync(ct);

        return account;
    }

    public async Task<Account> AddAsync(Account account, CancellationToken ct) =>
        (await _dbContext.AddAsync(account, ct)).Entity;

    public void Update(Account account) =>
        _dbContext.Entry(account).State = EntityState.Modified;
}
