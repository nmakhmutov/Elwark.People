using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace People.Api.Infrastructure.Provider.Email
{
    public interface IEmailSender
    {
        public Task SendEmailAsync(MailAddress email, string subject, string body, CancellationToken ct = default);
    }
}
