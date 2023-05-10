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

    public async Task SendAll(IEnumerable<WebhookSubscription> receivers, WebhookData data)
    {
        var json = JsonSerializer.Serialize(data, Options);
        var tasks = receivers.Select(x => OnSendData(x, json));

        await Task.WhenAll(tasks);
    }

    private async Task<HttpResponseMessage> OnSendData(WebhookSubscription subs, string json)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(subs.DestinationUrl, UriKind.Absolute),
            Method = HttpMethod.Post,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(subs.Token))
            request.Headers.TryAddWithoutValidation("X-Elwark-People-Token", subs.Token);

        _logger.LogInformation("Sending hook to {Method} {Uri} {json}", request.Method, subs.DestinationUrl, json);

        var response = await _httpClient.SendAsync(request);

        _logger.LogInformation("Hook to {Method} {Uri} sent {StatusCode}", request.Method, subs.DestinationUrl,
            (int)response.StatusCode);

        return response;
    }
}
