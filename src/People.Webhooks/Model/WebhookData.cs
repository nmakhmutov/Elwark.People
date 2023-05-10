namespace People.Webhooks.Model;

public sealed record WebhookData(long AccountId, WebhookType Type, DateTime CreatedAt);
