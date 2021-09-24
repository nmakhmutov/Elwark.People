using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace People.Notification.Api.Infrastructure.Provider
{
    public interface IEmailSender
    {
        public Task SendEmailAsync(MailAddress email, string subject, string body, CancellationToken ct = default);
    }
}
