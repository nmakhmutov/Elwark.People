using System.Net.Mail;
using People.Domain.SeedWork;

namespace People.Infrastructure.Confirmations;

public interface IConfirmationService
{
    Task<Result<AccountConfirmation>> CheckSignInAsync(string token, int code);

    Task<ConfirmationResult> CreateSignInAsync(long id, ITimeProvider time);

    Task<Result<AccountConfirmation>> CheckSignUpAsync(string token, int code);

    Task<ConfirmationResult> CreateSignUpAsync(long id, ITimeProvider time);

    Task<Result<EmailConfirmation>> CheckEmailVerifyAsync(string token, int code);

    Task<ConfirmationResult> CreateEmailVerifyAsync(long id, MailAddress email, ITimeProvider time);
}
