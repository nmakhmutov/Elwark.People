using NSubstitute;
using People.Application.Commands.SendWebhooks;
using People.Application.Providers.Webhooks;
using People.Infrastructure.Webhooks;
using Integration.Shared.Tests.Infrastructure;
using Xunit;

namespace Integration.Api.Tests.Commands;

[Collection(nameof(PostgresCollection))]
public sealed class SendWebhooksCommandTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Handle_WhenSubscriptionsMatch_SendsWebhooksWithFilteredRows()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);

        await db.Webhooks.AddRangeAsync(
            new Webhook(WebhookType.Created, WebhookMethod.Post, "https://hooks.example/created", null),
            new Webhook(WebhookType.Updated, WebhookMethod.Post, "https://hooks.example/updated", null)
        );

        await db.SaveChangesAsync();

        var sender = Substitute.For<IWebhookSender>();
        var handler = new SendWebhooksCommandHandler(new WebhookRetriever(db), sender);
        var occurred = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

        _ = await handler.Handle(new SendWebhooksCommand(77L, WebhookType.Created, occurred), CancellationToken.None);

        await sender.Received(1)
            .SendAsync(
                77L,
                occurred,
                Arg.Is<IEnumerable<Webhook>>(list => list.Count() == 1 && list.First().Type == WebhookType.Created),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Handle_WhenNoSubscriptionsMatch_DoesNotCallSender()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);

        await db.Webhooks.AddRangeAsync(
            new Webhook(WebhookType.Updated, WebhookMethod.Post, "https://hooks.example/u", null)
        );

        await db.SaveChangesAsync();

        var sender = Substitute.For<IWebhookSender>();
        var handler = new SendWebhooksCommandHandler(new WebhookRetriever(db), sender);

        _ = await handler.Handle(new SendWebhooksCommand(1L, WebhookType.Deleted, DateTime.UtcNow), CancellationToken.None);

        await sender.DidNotReceive()
            .SendAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<Webhook>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTableEmpty_IsNoOp()
    {
        await using var db = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);

        var sender = Substitute.For<IWebhookSender>();
        var handler = new SendWebhooksCommandHandler(new WebhookRetriever(db), sender);

        _ = await handler.Handle(new SendWebhooksCommand(2L, WebhookType.Created, DateTime.UtcNow),
            CancellationToken.None);

        await sender.DidNotReceive()
            .SendAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<Webhook>>(),
                Arg.Any<CancellationToken>());
    }
}
