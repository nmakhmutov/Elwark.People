using System.Runtime.CompilerServices;
using Common.Kafka;
using Integration.Event;
using MongoDB.Driver;
using Notification.Api.Infrastructure;
using Notification.Api.Models;
using Quartz;

namespace Notification.Api.Job;

[DisallowConcurrentExecution]
public sealed class PostponedEmailJob : IJob
{
    private readonly IKafkaMessageBus _bus;
    private readonly NotificationDbContext _dbContext;

    public PostponedEmailJob(IKafkaMessageBus bus, NotificationDbContext dbContext)
    {
        _bus = bus;
        _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var update = Builders<PostponedEmail>.Update.Inc(x => x.Version, 1);
        var options = new FindOneAndUpdateOptions<PostponedEmail>();

        await foreach (var (find, delete) in GetFiltersAsync(context.FireTimeUtc.UtcDateTime, context.CancellationToken))
        {
            var result = await _dbContext.PostponedEmails
                .FindOneAndUpdateAsync(find, update, options, context.CancellationToken);

            if (result is null)
                continue;

            var evt = EmailMessageCreatedIntegrationEvent.CreateDurable(result.Email, result.Subject, result.Body);
            await _bus.PublishAsync(evt, context.CancellationToken);

            await _dbContext.PostponedEmails.DeleteOneAsync(delete, context.CancellationToken);
        }
    }

    private async IAsyncEnumerable<Filters> GetFiltersAsync(DateTime sendAt, [EnumeratorCancellation] CancellationToken ct)
    {
        using var cursor = await _dbContext.PostponedEmails
            .Find(Builders<PostponedEmail>.Filter.Lt(x => x.SendAt, sendAt))
            .Sort(Builders<PostponedEmail>.Sort.Descending(x => x.SendAt))
            .Project(x => new { x.Id, x.Version })
            .ToCursorAsync(ct);

        while (await cursor.MoveNextAsync(ct))
            foreach (var item in cursor.Current)
                yield return new Filters(
                    Builders<PostponedEmail>.Filter.And(
                        Builders<PostponedEmail>.Filter.Eq(x => x.Id, item.Id),
                        Builders<PostponedEmail>.Filter.Eq(x => x.Version, item.Version)
                    ),
                    Builders<PostponedEmail>.Filter.Eq(x => x.Id, item.Id)
                );
    }

    private sealed record Filters(FilterDefinition<PostponedEmail> Find, FilterDefinition<PostponedEmail> Delete);
}
