using System.Net.Mail;
using People.Domain.Entities;

namespace People.Infrastructure.Confirmations;

public interface IConfirmationService
{
    Task<ConfirmationResult> SignInAsync(AccountId id, CancellationToken ct = default);

    Task<AccountId> SignInAsync(string token, string code, CancellationToken ct = default);

    Task<ConfirmationResult> SignUpAsync(AccountId id, CancellationToken ct = default);

    Task<AccountId> SignUpAsync(string token, string code, CancellationToken ct = default);

    Task<ConfirmationResult> VerifyEmailAsync(AccountId id, MailAddress email, CancellationToken ct = default);

    Task<EmailConfirmation> VerifyEmailAsync(string token, string code, CancellationToken ct = default);

    Task<int> DeleteAsync(AccountId id, CancellationToken ct = default);

    Task<int> CleanUpAsync(CancellationToken ct = default);
}
