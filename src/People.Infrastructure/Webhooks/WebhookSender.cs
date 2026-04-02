using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using People.Application.Providers.Webhooks;

namespace People.Infrastructure.Webhooks;

internal sealed class WebhookRetriever : IWebhookRetriever
{
    private readonly PeopleDbContext _dbContext;

    public WebhookRetriever(PeopleDbContext dbContext) =>
        _dbContext = dbContext;

    public async IAsyncEnumerable<Webhook> RetrieveAsync(
        WebhookType type,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var query = _dbContext.Webhooks
            .AsNoTracking()
            .Where(w => w.Type == type)
            .AsAsyncEnumerable();

        await foreach (var webhook in query.WithCancellation(ct))
            yield return webhook;
    }
}

internal sealed class WebhookSender(HttpClient httpClient, ILogger<WebhookSender> logger) : IWebhookSender
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

        logger.LogInformation("Initiating webhook transmission to {Method} {Url}", request.Method, request.RequestUri);

        var response = await httpClient.SendAsync(request, ct);

        logger.LogInformation(
            "Webhook transmission to {Method} {Url} completed with Status Code: {StatusCode}",
            request.Method,
            request.RequestUri,
            response.StatusCode);
    }

    [UsedImplicitly]
    private readonly record struct WebhookPayload(long AccountId, DateTime CreatedAt);
}
