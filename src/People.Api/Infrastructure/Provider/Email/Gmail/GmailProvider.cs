using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace People.Api.Infrastructure.Provider.Email.Gmail
{
    internal sealed class GmailProvider : IEmailSender
    {
        private readonly GmailOptions _options;

        public GmailProvider(IOptions<GmailOptions> options) =>
            _options = options.Value;
        
        public async Task SendEmailAsync(MailAddress email, string subject, string body, CancellationToken ct)
        {
            using var message = new MailMessage(_options.Username, email.Address, subject, body)
            {
                IsBodyHtml = true
            };

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            await smtp.SendMailAsync(message, ct);
        }
    }
}
