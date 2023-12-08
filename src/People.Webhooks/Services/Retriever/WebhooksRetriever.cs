using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using People.Webhooks.Infrastructure;
using People.Webhooks.Model;

namespace People.Webhooks.Services.Retriever;

internal sealed class WebhooksRetriever : IWebhooksRetriever
{
    private readonly WebhookDbContext _dbContext;

    public WebhooksRetriever(WebhookDbContext dbContext) =>
        _dbContext = dbContext;

    public async IAsyncEnumerable<WebhookSubscription> GetSubscribersAsync(WebhookType type,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var query = _dbContext.Subscriptions
            .Where(x => x.Type == type)
            .AsAsyncEnumerable();

        await foreach (var item in query.WithCancellation(ct))
            yield return item;
    }
}
