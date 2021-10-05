using System.Net;
using System.Net.Mail;

namespace Notification.Api.Infrastructure.Provider.Gmail;

internal sealed class GmailProvider : IEmailSender
{
    private readonly string _password;
    private readonly string _userName;

    public GmailProvider(string userName, string password)
    {
        _userName = userName;
        _password = password;
    }

    public async Task SendEmailAsync(MailAddress email, string subject, string body, CancellationToken ct)
    {
        using var message = new MailMessage(_userName, email.Address, subject, body)
        {
            IsBodyHtml = true
        };

        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(_userName, _password),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        await smtp.SendMailAsync(message, ct);
    }
}
