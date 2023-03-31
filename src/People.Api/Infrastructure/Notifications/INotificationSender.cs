using System.Net.Mail;
using People.Domain.ValueObjects;

namespace People.Api.Infrastructure.Notifications;

public interface INotificationSender
{
    Task SendConfirmationAsync(MailAddress email, string code, Language language, CancellationToken ct = default);
}
