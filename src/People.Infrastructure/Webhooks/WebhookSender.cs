using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using People.Application.Providers.Webhooks;

namespace People.Infrastructure.Webhooks;

internal sealed partial class WebhookSender(HttpClient httpClient, ILogger<WebhookSender> logger) : IWebhookSender
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task SendAsync(long accountId, DateTime occurredAt, IEnumerable<Webhook> subscriptions, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new WebhookPayload(accountId, occurredAt), Options);
        return Task.WhenAll(subscriptions.Select(s => SendOneAsync(s, json, ct)));
    }

    private async Task SendOneAsync(Webhook subscription, string json, CancellationToken ct)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(subscription.DestinationUrl, UriKind.Absolute),
            Method = subscription.Method switch
            {
                WebhookMethod.Post => HttpMethod.Post,
                WebhookMethod.Put => HttpMethod.Put,
                WebhookMethod.Delete => HttpMethod.Delete,
                _ => throw new UnreachableException($"Unknown webhook method {subscription.Method}")
            },
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(subscription.Token))
            request.Headers.TryAddWithoutValidation("X-Elwark-People-Token", subscription.Token);

        WebhookSending(request.Method, request.RequestUri);

        var response = await httpClient.SendAsync(request, ct);

        WebhookSent(request.Method, request.RequestUri, response.StatusCode);
    }

    [UsedImplicitly]
    private readonly record struct WebhookPayload(long AccountId, DateTime CreatedAt);

    [LoggerMessage(LogLevel.Information, "Webhook to {Method} {Url} completed with Status Code: {StatusCode}")]
    private partial void WebhookSent(HttpMethod method, Uri url, HttpStatusCode statusCode);

    [LoggerMessage(LogLevel.Information, "Initiating webhook to {Method} {Url}")]
    partial void WebhookSending(HttpMethod method, Uri url);
}
