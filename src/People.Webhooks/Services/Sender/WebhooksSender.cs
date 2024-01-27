using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using People.Webhooks.Model;

namespace People.Webhooks.Services.Sender;

internal sealed class WebhooksSender : IWebhooksSender
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhooksSender> _logger;

    public WebhooksSender(HttpClient httpClient, ILogger<WebhooksSender> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendAll(IEnumerable<WebhookSubscription> subscriptions, WebhookData data)
    {
        var json = JsonSerializer.Serialize(data, Options);
        var tasks = subscriptions.Select(async x =>
        {
            _logger.LogInformation("Initiating webhook transmission to {Url}. Payload: {Json}", x.DestinationUrl, data);

            var response = await SendDataAsync(x, json);

            _logger.LogInformation("Webhook transmission to {Url} completed with Status Code: {StatusCode}",
                x.DestinationUrl, response.StatusCode);

            return response;
        });

        await Task.WhenAll(tasks);
    }

    private async Task<HttpResponseMessage> SendDataAsync(WebhookSubscription subscription, string json)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(subscription.DestinationUrl, UriKind.Absolute),
            Method = HttpMethod.Post,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(subscription.Token))
            request.Headers.TryAddWithoutValidation("X-Elwark-People-Token", subscription.Token);

        return await _httpClient.SendAsync(request);
    }
}
