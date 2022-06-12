using System.Net.Mail;

namespace People.Infrastructure.Confirmations;

public sealed record EmailConfirmation(long AccountId, MailAddress Email);
