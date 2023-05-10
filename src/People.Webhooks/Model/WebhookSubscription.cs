// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace People.Webhooks.Model;

public sealed class WebhookSubscription
{
    public static WebhookSubscription Create(WebhookType type, Uri destinationUrl, string? token = null)
    {
        if (!destinationUrl.IsAbsoluteUri)
            throw new ArgumentException("Destination url must be an absolute url");

        return new WebhookSubscription(type, destinationUrl.ToString(), token);
    }

    private WebhookSubscription(WebhookType type, string destinationUrl, string? token)
    {
        Type = type;
        DestinationUrl = destinationUrl;
        Token = token;
    }

    public int Id { get; private set; }

    public WebhookType Type { get; private set; }

    public string DestinationUrl { get; private set; }

    public string? Token { get; private set; }
}
