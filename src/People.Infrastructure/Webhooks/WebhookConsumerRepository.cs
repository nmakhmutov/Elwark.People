using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using People.Application.Webhooks;

namespace People.Infrastructure.Webhooks;

internal sealed class WebhookConsumerRepository : IWebhookConsumerRepository
{
    private readonly WebhookDbContext _dbContext;

    public WebhookConsumerRepository(WebhookDbContext dbContext) =>
        _dbContext = dbContext;

    public Task<WebhookConsumer?> GetAsync(Guid id, CancellationToken ct) =>
        _dbContext.Consumers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async IAsyncEnumerable<WebhookConsumer> GetAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var query = _dbContext.Consumers
            .AsNoTracking()
            .AsAsyncEnumerable();

        await foreach (var consumer in query.WithCancellation(ct))
            yield return consumer;
    }

    public async Task<WebhookConsumer> CreateAsync(WebhookConsumer consumer, CancellationToken ct)
    {
        _dbContext.Consumers.Add(consumer);
        await _dbContext.SaveChangesAsync(ct);

        return consumer;
    }

    public async Task<WebhookConsumer> UpdateAsync(WebhookConsumer consumer, CancellationToken ct)
    {
        _dbContext.Consumers.Update(consumer);
        await _dbContext.SaveChangesAsync(ct);

        return consumer;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct) =>
        _dbContext.Consumers
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(ct);
}
