using System.Net.Mail;

namespace People.Application.Providers.Confirmation;

public interface IEmailVerificationTokenService
{
    string CreateToken(Guid confirmationId, MailAddress email);

    EmailVerificationTokenPayload ParseToken(string token);
}
