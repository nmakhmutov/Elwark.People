using Microsoft.EntityFrameworkCore;
using People.Webhooks.Infrastructure;
using People.Webhooks.Model;

namespace People.Webhooks.Services.Retriever;

internal sealed class WebhooksRetriever : IWebhooksRetriever
{
    private readonly WebhookDbContext _dbContext;

    public WebhooksRetriever(WebhookDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<IReadOnlyCollection<WebhookSubscription>> GetSubscribersAsync(WebhookType type) =>
        await _dbContext.Subscriptions
            .Where(x => x.Type == type)
            .ToArrayAsync();
}
