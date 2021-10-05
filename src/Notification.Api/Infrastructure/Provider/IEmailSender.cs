using System.Net.Mail;

namespace Notification.Api.Infrastructure.Provider;

public interface IEmailSender
{
    public Task SendEmailAsync(MailAddress email, string subject, string body, CancellationToken ct = default);
}
