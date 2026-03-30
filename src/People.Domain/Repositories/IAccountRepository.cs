using System.Net.Mail;
using People.Domain.Entities;
using People.Domain.SeedWork;

namespace People.Domain.Repositories;

public interface IAccountRepository : IRepository<Account>
{
    Task<bool> IsExistsAsync(MailAddress email, CancellationToken ct = default);

    Task<bool> IsExistsAsync(ExternalService service, string identity, CancellationToken ct = default);

    Task<Account?> GetAsync(AccountId id, CancellationToken ct = default);

    Task<ExternalSignInMatch?> GetAsync(ExternalService service, string identity, CancellationToken ct = default);

    Task<EmailSignupState?> GetEmailSignupStateAsync(MailAddress email, CancellationToken ct = default);

    Task<ExternalSignInMatch?> GetSignInMatchAsync(AccountId id, CancellationToken ct = default);
}
