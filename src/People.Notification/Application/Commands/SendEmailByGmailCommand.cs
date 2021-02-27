using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using People.Notification.Options;

namespace People.Notification.Application.Commands
{
    public sealed record SendEmailByGmailCommand(string Email, string Subject, string Body) : IRequest;

    internal sealed class SendEmailByGmailCommandHandler : IRequestHandler<SendEmailByGmailCommand>
    {
        private readonly GmailOptions _options;

        public SendEmailByGmailCommandHandler(IOptions<GmailOptions> options) =>
            _options = options.Value;

        public async Task<Unit> Handle(SendEmailByGmailCommand request, CancellationToken cancellationToken)
        {
            using var message = new MailMessage(_options.Username, request.Email, request.Subject, request.Body)
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

            await smtp.SendMailAsync(message, cancellationToken);

            return Unit.Value;
        }
    }
}