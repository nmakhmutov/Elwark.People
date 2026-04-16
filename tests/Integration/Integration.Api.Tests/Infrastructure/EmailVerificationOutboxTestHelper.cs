using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using People.Application.Providers;
using People.Application.Providers.Confirmation;
using People.Domain.IntegrationEvents;
using People.Infrastructure;

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

        var processedAt = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        foreach (var message in messages)
        {
            if (message.GetPayload() is not EmailVerificationRequestedIntegrationEvent payload)
                continue;

            var code = await db.Confirmations
                .AsNoTracking()
                .Where(x => x.AccountId == payload.AccountId && x.Type == ConfirmationType.EmailConfirmation)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Code)
                .FirstOrDefaultAsync(ct);

            if (code is null)
                continue;

            await notification.SendConfirmationAsync(new MailAddress(payload.Email), code, payload.Locale, ct);

            message.MarkProcessed(processedAt);
        }

        await db.SaveChangesAsync(ct);
    }
}
