namespace People.Application.Webhooks;

public sealed class WebhookConsumer
{
    public Guid Id { get; private set; }

    public WebhookType Type { get; private set; }

    public WebhookMethod Method { get; private set; }

    public string DestinationUrl { get; private set; }

    public string? Token { get; private set; }

    public static WebhookConsumer Create(WebhookType type, WebhookMethod method, string destination, string? token) =>
        new(Guid.CreateVersion7(), type, method, destination, token);

    private WebhookConsumer(Guid id, WebhookType type, WebhookMethod method, string destinationUrl, string? token)
    {
        Id = id;
        Type = type;
        Method = method;
        DestinationUrl = destinationUrl;
        Token = token;
    }

    public void Update(WebhookType type, WebhookMethod method, string destinationUrl, string? token)
    {
        Type = type;
        Method = method;
        DestinationUrl = destinationUrl;
        Token = token;
    }
}
