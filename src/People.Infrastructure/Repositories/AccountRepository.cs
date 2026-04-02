using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;

namespace People.Infrastructure.Repositories;

internal sealed class AccountRepository : IAccountRepository
{
    private readonly PeopleDbContext _dbContext;

    public IUnitOfWork UnitOfWork =>
        _dbContext;

    public AccountRepository(PeopleDbContext dbContext) =>
        _dbContext = dbContext;

    public Task<bool> IsExistsAsync(MailAddress email, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(email);

        return _dbContext.Emails.AnyAsync(x => x.Email == email.Address, ct);
    }

    public Task<bool> IsExistsAsync(ExternalService service, string identity, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(identity);

        return _dbContext.Connections.AnyAsync(x => x.Type == service && x.Identity == identity, ct);
    }

    public Task<Account?> GetAsync(AccountId id, CancellationToken ct) =>
        _dbContext.Accounts
            .AsSplitQuery()
            .Include(x => x.Emails)
            .Include(x => x.Externals)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Account?> GetAsync(ExternalService service, string identity, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(identity);

        return _dbContext.Accounts
            .AsSplitQuery()
            .Include(x => x.Emails)
            .Include(x => x.Externals)
            .Where(x => x.Externals.Any(e => e.Type == service && e.Identity == identity))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Account> AddAsync(Account account, CancellationToken ct) =>
        (await _dbContext.Accounts.AddAsync(account, ct)).Entity;

    public void Delete(Account entity) =>
        _dbContext.Accounts.Remove(entity);

    public async Task<EmailSignupState?> GetEmailSignupStateAsync(MailAddress email, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(email);

        var row = await _dbContext.Emails
            .AsNoTracking()
            .Where(x => x.Email == email.Address)
            .Select(x => new
            {
                x.AccountId,
                x.Email,
                ConfirmedAt = EF.Property<DateTime?>(x, "_confirmedAt")
            })
            .FirstOrDefaultAsync(ct);

        return row is null
            ? null
            : new EmailSignupState(row.AccountId, new MailAddress(row.Email), row.ConfirmedAt.HasValue);
    }
}
