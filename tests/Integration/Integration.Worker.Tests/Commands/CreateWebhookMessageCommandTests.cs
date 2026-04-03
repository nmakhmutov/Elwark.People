using Integration.Shared.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using People.Application.Webhooks;
using People.Infrastructure.Webhooks;
using People.Worker.Commands;
using Xunit;

namespace Integration.Worker.Tests.Commands;

[Collection(nameof(PostgresCollection))]
public sealed class CreateWebhookMessageCommandTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Handle_CreatesWebhookMessageRow()
    {
        await using var webhookDb = fixture.CreateWebhookContext();
        await webhookDb.Messages.ExecuteDeleteAsync();

        var occurred = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        var command = new CreateWebhookMessageCommand(42L, WebhookType.Created, occurred);
        var handler = new CreateWebhookMessageCommandHandler(webhookDb);

        await handler.Handle(command, CancellationToken.None);

        var messages = await webhookDb.Messages.ToListAsync();
        var message = Assert.Single(messages);
        Assert.Equal(42L, message.AccountId);
        Assert.Equal(WebhookType.Created, message.Type);
        Assert.Equal(occurred, message.OccurredAt);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.RetryAfter);
        Assert.Equal(WebhookStatus.Pending, message.Status);
    }
}
