using JetBrains.Annotations;

namespace People.Application.Providers.Webhooks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class Webhook
{
    public int Id { get; private set; }

    public WebhookType Type { get; private set; }

    public WebhookMethod Method { get; private set; }

    public string DestinationUrl { get; private set; }

    public string? Token { get; private set; }

    public Webhook(WebhookType type, WebhookMethod method, string destinationUrl, string? token)
        : this(0, type, method, destinationUrl, token)
    {
    }

    private Webhook(int id, WebhookType type, WebhookMethod method, string destinationUrl, string? token)
    {
        Id = id;
        Type = type;
        Method = method;
        DestinationUrl = destinationUrl;
        Token = token;
    }
}
