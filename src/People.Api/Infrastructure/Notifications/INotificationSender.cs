using System.Net.Mail;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Api.Infrastructure.Notifications;

public interface INotificationSender
{
    Task SendConfirmationAsync(MailAddress email, int code, Language language, CancellationToken ct = default);
}
