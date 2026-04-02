using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using People.Api.Application.Commands.SendWebhooks;
using People.Api.Infrastructure.Services;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Infrastructure.Webhooks;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Commands;

[Collection(nameof(PostgresCollection))]
public sealed class SendWebhooksCommandTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Handle_WhenSubscriptionsMatch_SendsWebhooksWithFilteredRows()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO webhook_subscriptions (type, method, destination_url, token)
             VALUES ({(byte)WebhookType.Created}, {(byte)WebhookMethod.Post}, {"https://hooks.example/created"}, {null}),
                    ({(byte)WebhookType.Updated}, {(byte)WebhookMethod.Post}, {"https://hooks.example/updated"}, {null})
             """);

        var sender = Substitute.For<IWebhookSender>();
        var handler = new SendWebhooksCommandHandler(db, sender);
        var occurred = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

        _ = await handler.Handle(new SendWebhooksCommand(77L, WebhookType.Created, occurred), CancellationToken.None);

        await sender.Received(1).SendAsync(
            77L,
            occurred,
            Arg.Is<IEnumerable<WebhookSubscription>>(list =>
            {
                var items = list.ToList();
                return items.Count == 1 && items[0].Type == WebhookType.Created;
            }),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoSubscriptionsMatch_DoesNotCallSender()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO webhook_subscriptions (type, method, destination_url, token)
             VALUES ({(byte)WebhookType.Updated}, {(byte)WebhookMethod.Post}, {"https://hooks.example/u"}, {null})
             """);

        var sender = Substitute.For<IWebhookSender>();
        var handler = new SendWebhooksCommandHandler(db, sender);

        _ = await handler.Handle(
            new SendWebhooksCommand(1L, WebhookType.Deleted, DateTime.UtcNow),
            CancellationToken.None);

        await sender.DidNotReceive().SendAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<WebhookSubscription>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTableEmpty_IsNoOp()
    {
        await using var db = fixture.CreateContext(new NoOpMediator());
        await IntegrationDatabaseCleanup.DeleteAllAsync(db);

        var sender = Substitute.For<IWebhookSender>();
        var handler = new SendWebhooksCommandHandler(db, sender);

        _ = await handler.Handle(new SendWebhooksCommand(2L, WebhookType.Created, DateTime.UtcNow), CancellationToken.None);

        await sender.DidNotReceive().SendAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<IEnumerable<WebhookSubscription>>(), Arg.Any<CancellationToken>());
    }
}
