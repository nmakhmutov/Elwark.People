using System.Net.Mail;

namespace Notification.Api.Infrastructure.Provider.SendGrid;

// ReSharper disable NotAccessedPositionalProperty.Local
internal sealed class SendgridProvider : IEmailSender
{
    private readonly HttpClient _client;

    public SendgridProvider(HttpClient client) =>
        _client = client;

    public async Task SendEmailAsync(MailAddress email, string subject, string body, CancellationToken ct)
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

        var response = await _client.PostAsJsonAsync("v3/mail/send", new Message(from, to, subject, content), ct);
        response.EnsureSuccessStatusCode();
    }

    private sealed record EmailAddress(string Email, string? Name);

    private sealed record Personalization(EmailAddress[] To);

    private sealed record Content(string Type, string Value);

    private sealed record Message(EmailAddress From, Personalization[] Personalizations, string Subject,
        Content[] Content);
}
