using System.Collections.Frozen;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using People.Application.Providers;
using People.Domain.ValueObjects;
using People.Infrastructure.Email.Models;
using People.Infrastructure.EmailBuilder;

namespace People.Infrastructure.Providers;

internal sealed partial class NotificationSender : INotificationSender
{
    private static readonly FrozenSet<string> Languages =
        new[] { "en", "ru" }.ToFrozenSet();

    private readonly HttpClient _client;
    private readonly IEmailBuilder _emailBuilder;
    private readonly ILogger<NotificationSender> _logger;

    public NotificationSender(HttpClient client, IEmailBuilder emailBuilder, ILogger<NotificationSender> logger)
    {
        _client = client;
        _emailBuilder = emailBuilder;
        _logger = logger;
    }

    public async Task SendConfirmationAsync(MailAddress email, string code, Locale locale, CancellationToken ct)
    {
        LogSendingConfirmation(email.Address);

        var language = Languages.Contains(locale.Language) ? locale.Language : "en";
        var template = $"Confirmation.{language}.liquid";
        var (subject, body) = await _emailBuilder.CreateEmailAsync(template, new ConfirmationCodeModel(code));

        var path = $"/emails/{Uri.EscapeDataString(email.Address)}?subject={subject}";
        using var content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Text.Html);
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = content;

        using var response = await _client.SendAsync(request, ct);

        LogConfirmationSentToEmailByUrlWithStatusCode(email.Address, request.RequestUri, response.StatusCode);

        response.EnsureSuccessStatusCode();
    }

    [LoggerMessage(LogLevel.Information, "Sending confirmation to {email}")]
    partial void LogSendingConfirmation(string email);

    [LoggerMessage(LogLevel.Information, "Confirmation sent to {email} by {url} with status {code}")]
    partial void LogConfirmationSentToEmailByUrlWithStatusCode(string email, Uri? url, HttpStatusCode code);
}
