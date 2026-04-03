using Microsoft.EntityFrameworkCore;
using People.Application.Providers;
using People.Domain.IntegrationEvents;
using People.Infrastructure;
using People.Worker.Commands;

namespace Integration.Api.Tests.Infrastructure;

internal static class EmailVerificationOutboxTestHelper
{
    internal static async Task DispatchPendingAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        var notification = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        var messages = await db.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        var handler = new SendEmailVerificationCommandHandler(db, notification);
        var processedAt = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        foreach (var message in messages)
        {
            if (message.GetPayload() is not EmailVerificationRequestedIntegrationEvent payload)
                continue;

            await handler.Handle(
                new SendEmailVerificationCommand(
                    payload.AccountId,
                    payload.ConfirmationId,
                    payload.Email,
                    payload.Language,
                    payload.OccurredAt),
                ct);

            message.MarkProcessed(processedAt);
        }

        await db.SaveChangesAsync(ct);
    }
}
