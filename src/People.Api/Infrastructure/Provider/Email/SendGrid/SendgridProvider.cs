using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace People.Api.Infrastructure.Provider.Email.SendGrid
{
    internal class SendgridProvider : IEmailSender
    {
        private readonly HttpClient _client;

        public SendgridProvider(HttpClient client) =>
            _client = client;

        public Task SendEmailAsync(MailAddress email, string subject, string body, CancellationToken ct)
        {
            var from = new EmailAddress("elwarkinc@gmail.com", "Elwark");
            var to = new Personalization[]
            {
                new(new EmailAddress[]
                {
                    new(email.Address, null)
                })
            };
            var content = new Content[]
            {
                new("text/html", body)
            };

            return _client.PostAsJsonAsync("v3/mail/send", new Message(from, to, subject, content), ct);
        }

        private sealed record EmailAddress(string Email, string? Name);

        private sealed record Personalization(EmailAddress[] To);

        private sealed record Content(string Type, string Value);

        private sealed record Message(EmailAddress From, Personalization[] Personalizations, string Subject,
            Content[] Content);
    }
}
