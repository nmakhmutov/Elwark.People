using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using People.Api.Email.Models;
using People.Api.Infrastructure.EmailBuilder;
using People.Domain.ValueObjects;

namespace People.Api.Infrastructure.Notifications;

internal sealed partial class NotificationSender : INotificationSender
{
    private readonly HttpClient _client;
    private readonly IEmailBuilder _emailBuilder;
    private readonly ILogger<NotificationSender> _logger;

    public NotificationSender(HttpClient client, IEmailBuilder emailBuilder, ILogger<NotificationSender> logger)
    {
        _client = client;
        _emailBuilder = emailBuilder;
        _logger = logger;
    }

    public async Task SendConfirmationAsync(MailAddress email, string code, Language language, CancellationToken ct)
    {
        LogSendingConfirmation(email.Address);

        var template = $"Confirmation.{language}.liquid";
        var (subject, body) = await _emailBuilder.CreateEmailAsync(template, new ConfirmationCodeModel(code));

        var path = QueryHelpers.AddQueryString($"/emails/{Uri.EscapeDataString(email.Address)}", "subject", subject);
        using var content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Text.Html);
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content
        };
        using var response = await _client.SendAsync(request, ct);

        LogConfirmationSentToEmailByUrlWithStatusCode(email.Address, request.RequestUri, response.StatusCode);

        response.EnsureSuccessStatusCode();
    }

    [LoggerMessage(LogLevel.Information, "Sending confirmation to {email}")]
    partial void LogSendingConfirmation(string email);

    [LoggerMessage(LogLevel.Information, "Confirmation sent to {email} by {url} with status {code}")]
    partial void LogConfirmationSentToEmailByUrlWithStatusCode(string email, Uri? url, HttpStatusCode code);
}
