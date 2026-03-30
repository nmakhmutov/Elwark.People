using System.Net.Mail;
using People.Domain.Entities;

namespace People.Domain.Repositories;

public sealed record EmailSignupState(AccountId AccountId, MailAddress Email, bool IsConfirmed);
