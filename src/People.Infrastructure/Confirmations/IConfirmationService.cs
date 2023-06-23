using System.Net.Mail;
using People.Domain.SeedWork;

namespace People.Infrastructure.Confirmations;

public interface IConfirmationService
{
    Task<ConfirmationResult> SignInAsync(long id, ITimeProvider time, CancellationToken ct = default);

    Task<AccountConfirmation> SignInAsync(string token, string code, CancellationToken ct = default);

    Task<ConfirmationResult> SignUpAsync(long id, ITimeProvider time, CancellationToken ct = default);

    Task<AccountConfirmation> SignUpAsync(string token, string code, CancellationToken ct = default);

    Task<ConfirmationResult> VerifyEmailAsync(long id, MailAddress email, ITimeProvider time,
        CancellationToken ct = default);

    Task<EmailConfirmation> VerifyEmailAsync(string token, string code, CancellationToken ct = default);

    Task<int> DeleteAsync(DateTime now, CancellationToken ct = default);

    Task<int> DeleteAsync(long id, CancellationToken ct = default);
}
