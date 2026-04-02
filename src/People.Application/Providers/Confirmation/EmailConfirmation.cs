using System.Net.Mail;
using People.Domain.Entities;

namespace People.Application.Providers.Confirmation;

public sealed record EmailConfirmation(AccountId AccountId, MailAddress Email);
