using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace People.Notification.Services
{
    public class SendgridProvider : ISendgridProvider
    {
        private readonly HttpClient _client;

        public SendgridProvider(HttpClient client) =>
            _client = client;

        public async Task SendEmailAsync(string email, string subject, string body)
        {
            var message = new Message(
                new EmailAddress("elwarkinc@gmail.com", "Elwark"),
                new Personalization[]
                {
                    new(new EmailAddress[]
                    {
                        new(email, null)
                    })
                },
                subject,
                new Content[]
                {
                    new("text/html", body)
                }
            );

            await _client.PostAsJsonAsync("v3/mail/send", message);
        }

        public sealed record EmailAddress(string Email, string? Name);

        public sealed record Personalization(EmailAddress[] To);

        public sealed record Content(string Type, string Value);

        public sealed record Message(
            EmailAddress From,
            Personalization[] Personalizations,
            string Subject,
            Content[] Content
        );
    }
}