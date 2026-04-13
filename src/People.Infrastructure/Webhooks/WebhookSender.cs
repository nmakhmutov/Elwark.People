using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using People.Application.Webhooks;

namespace People.Infrastructure.Webhooks;

internal sealed partial class WebhookSender(HttpClient httpClient, ILogger<WebhookSender> logger) : IWebhookSender
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task SendAsync(IEnumerable<WebhookConsumer> consumers, WebhookPayload payload, CancellationToken ct)
    {
        var json = JsonContent.Create(payload, options: Options);
        return Task.WhenAll(consumers.Select(s => SendOneAsync(s, json, ct)));
    }

    private async Task SendOneAsync(WebhookConsumer subscription, JsonContent json, CancellationToken ct)
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
            Content = json
        };

        if (!string.IsNullOrWhiteSpace(subscription.Token))
            request.Headers.TryAddWithoutValidation("X-Elwark-People-Token", subscription.Token);

        WebhookSending(request.Method, request.RequestUri);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        WebhookSent(request.Method, request.RequestUri, response.StatusCode);
    }

    [LoggerMessage(LogLevel.Information, "Webhook to {Method} {Url} completed with Status Code: {StatusCode}")]
    private partial void WebhookSent(HttpMethod method, Uri url, HttpStatusCode statusCode);

    [LoggerMessage(LogLevel.Information, "Initiating webhook to {Method} {Url}")]
    partial void WebhookSending(HttpMethod method, Uri url);
}

public readonly record struct WebhookPayload(long AccountId, DateTime CreatedAt);
