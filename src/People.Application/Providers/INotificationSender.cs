using System.Net.Mail;
using People.Domain.ValueObjects;

namespace People.Application.Providers;

public interface INotificationSender
{
    Task SendConfirmationAsync(MailAddress email, string code, Locale locale, CancellationToken ct = default);
}
