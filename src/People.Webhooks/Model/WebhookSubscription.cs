// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace People.Webhooks.Model;

public sealed class WebhookSubscription
{
    private WebhookSubscription(WebhookType type, WebhookMethod method, string destinationUrl, string? token)
    {
        Type = type;
        Method = method;
        DestinationUrl = destinationUrl;
        Token = token;
    }

    public int Id { get; private set; }

    public WebhookType Type { get; private set; }

    public WebhookMethod Method { get; private set; }

    public string DestinationUrl { get; private set; }

    public string? Token { get; private set; }

    public static WebhookSubscription Create(
        WebhookType type,
        WebhookMethod method,
        Uri destinationUrl,
        string? token = null
    )
    {
        if (!destinationUrl.IsAbsoluteUri)
            throw new ArgumentException("Destination url must be an absolute url");

        return new WebhookSubscription(type, method, destinationUrl.ToString(), token);
    }
}
