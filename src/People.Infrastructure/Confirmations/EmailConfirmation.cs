using System.Net.Mail;
using People.Domain.Entities;

namespace People.Infrastructure.Confirmations;

public sealed record EmailConfirmation(AccountId AccountId, MailAddress Email);
