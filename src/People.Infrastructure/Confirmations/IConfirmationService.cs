using System.Net.Mail;

namespace People.Infrastructure.Confirmations;

public interface IConfirmationService
{
    Task<ConfirmationResult> SignInAsync(long id, TimeProvider timeProvider, CancellationToken ct = default);

    Task<AccountConfirmation> SignInAsync(string token, string code, CancellationToken ct = default);

    Task<ConfirmationResult> SignUpAsync(long id, TimeProvider timeProvider, CancellationToken ct = default);

    Task<AccountConfirmation> SignUpAsync(string token, string code, CancellationToken ct = default);

    Task<ConfirmationResult> VerifyEmailAsync(long id, MailAddress email, TimeProvider timeProvider,
        CancellationToken ct = default);

    Task<EmailConfirmation> VerifyEmailAsync(string token, string code, CancellationToken ct = default);

    Task<int> DeleteAsync(DateTime now, CancellationToken ct = default);

    Task<int> DeleteAsync(long id, CancellationToken ct = default);
}
