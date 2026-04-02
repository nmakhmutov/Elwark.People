using JetBrains.Annotations;

namespace People.Application.Providers.Webhooks;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class Webhook
{
    public int Id { get; private set; }

    public WebhookType Type { get; private set; }

    public WebhookMethod Method { get; private set; }

    public string DestinationUrl { get; private set; } = null!;

    public string? Token { get; private set; }
}
