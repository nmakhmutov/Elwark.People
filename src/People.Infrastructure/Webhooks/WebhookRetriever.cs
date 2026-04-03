using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using People.Application.Providers.Webhooks;

namespace People.Infrastructure.Webhooks;

internal sealed class WebhookRetriever : IWebhookRetriever
{
    private readonly PeopleDbContext _dbContext;

    public WebhookRetriever(PeopleDbContext dbContext) =>
        _dbContext = dbContext;

    public async IAsyncEnumerable<Webhook> RetrieveAsync(
        WebhookType type,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var query = _dbContext.Webhooks
            .AsNoTracking()
            .Where(w => w.Type == type)
            .AsAsyncEnumerable();

        await foreach (var webhook in query.WithCancellation(ct))
            yield return webhook;
    }
}
