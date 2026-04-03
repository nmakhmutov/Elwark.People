using System.Net.Mail;

namespace People.Application.Providers.Confirmation;

public sealed record EmailVerificationTokenPayload(Guid ConfirmationId, MailAddress Email);
