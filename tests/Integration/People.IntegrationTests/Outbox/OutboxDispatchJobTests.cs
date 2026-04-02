using System.Threading;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using People.Api.Application.Commands.EnrichAccount;
using People.Api.Application.Commands.SendWebhooks;
using People.Domain.IntegrationEvents;
using People.Infrastructure;
using People.Infrastructure.Outbox;
using People.Infrastructure.Webhooks;
using People.IntegrationTests.Infrastructure;
using People.Worker.Jobs;
using Quartz;
using Xunit;

namespace People.IntegrationTests.Outbox;

[Collection(nameof(PostgresCollection))]
public sealed class OutboxDispatchJobTests(PostgreSqlFixture fixture)
{
    private static ServiceProvider CreateProvider(PostgreSqlFixture fx, ISender sender)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => sender);
        services.AddScoped<PeopleDbContext>(_ => fx.CreateContext(new NoOpMediator()));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static OutboxDispatchJob CreateJob(ServiceProvider root) =>
        new(root.GetRequiredService<IServiceScopeFactory>(), NullLogger<OutboxDispatchJob>.Instance);

    private static IJobExecutionContext FakeContext(CancellationToken ct = default)
    {
        var ctx = Substitute.For<IJobExecutionContext>();
        ctx.CancellationToken.Returns(ct);
        return ctx;
    }

    [Fact]
    public async Task Execute_DispatchesEnrichAndWebhooks_ForAccountCreatedPayload()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<EnrichAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Unit.Value));
        sender.Send(Arg.Any<SendWebhooksCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Unit.Value));

        await using var root = CreateProvider(fixture, sender);
        await using (var seedScope = root.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await IntegrationDatabaseCleanup.DeleteAllAsync(db);
            var evt = new AccountCreatedIntegrationEvent(
                Guid.CreateVersion7(),
                5001L,
                "198.51.100.10",
                DateTime.UtcNow.AddMinutes(-10));
            db.OutboxMessages.Add(OutboxMessage.Create(evt));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var runRoot = CreateProvider(fixture, sender))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await sender.Received(1).Send(
            Arg.Is<EnrichAccountCommand>(c => c.AccountId == 5001L && c.IpAddress == "198.51.100.10"),
            Arg.Any<CancellationToken>());
        await sender.Received(1).Send(
            Arg.Is<SendWebhooksCommand>(c =>
                c.AccountId == 5001L && c.Type == WebhookType.Created),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_OnHandlerFailure_SchedulesRetry_WithPendingStatus()
    {
        var sender = Substitute.For<ISender>();
        var calls = 0;
        sender.Send(Arg.Any<EnrichAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _ = Interlocked.Increment(ref calls);
                throw new InvalidOperationException("handler down");
            });

        Guid messageId;
        await using (var seedRoot = CreateProvider(fixture, sender))
        {
            await using var seedScope = seedRoot.CreateAsyncScope();
            var db = seedScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await IntegrationDatabaseCleanup.DeleteAllAsync(db);
            var evt = new AccountCreatedIntegrationEvent(
                Guid.CreateVersion7(),
                5002L,
                "198.51.100.11",
                DateTime.UtcNow.AddMinutes(-10));
            var msg = OutboxMessage.Create(evt);
            messageId = msg.Id;
            db.OutboxMessages.Add(msg);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var runRoot = CreateProvider(fixture, sender))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await using (var verifyRoot = CreateProvider(fixture, sender))
        {
            await using var scope = verifyRoot.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            var row = await db.OutboxMessages.SingleAsync(x => x.Id == messageId);
            Assert.Equal(OutboxStatus.Pending, row.Status);
            Assert.NotNull(row.NextRetryAt);
            Assert.Null(row.ProcessedAt);
        }
    }

    [Fact]
    public async Task Execute_PicksUpStalePendingMessage_WhenNextRetryAtIsNull_AndOccurredAtOlderThanFiveMinutes()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<EnrichAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Unit.Value));
        sender.Send(Arg.Any<SendWebhooksCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Unit.Value));

        await using var root = CreateProvider(fixture, sender);
        await using (var seedScope = root.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await IntegrationDatabaseCleanup.DeleteAllAsync(db);
            var evt = new AccountUpdatedIntegrationEvent(
                Guid.CreateVersion7(),
                5003L,
                DateTime.UtcNow.AddMinutes(-6));
            db.OutboxMessages.Add(OutboxMessage.Create(evt));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var runRoot = CreateProvider(fixture, sender))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await sender.Received(1).Send(
            Arg.Is<SendWebhooksCommand>(c => c.AccountId == 5003L && c.Type == WebhookType.Updated),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ProcessesAtMost100Messages_PerRun()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<EnrichAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Unit.Value));
        sender.Send(Arg.Any<SendWebhooksCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Unit.Value));

        await using var root = CreateProvider(fixture, sender);
        await using (var seedScope = root.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await IntegrationDatabaseCleanup.DeleteAllAsync(db);
            var messages = new List<OutboxMessage>();
            for (var i = 0; i < 101; i++)
            {
                messages.Add(
                    OutboxMessage.Create(
                        new AccountDeletedIntegrationEvent(
                            Guid.CreateVersion7(),
                            6000L + i,
                            DateTime.UtcNow.AddMinutes(-10))));
            }

            db.OutboxMessages.AddRange(messages);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var runRoot = CreateProvider(fixture, sender))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await using (var countRoot = CreateProvider(fixture, sender))
        {
            await using var scope = countRoot.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            var processed = await db.OutboxMessages.CountAsync(x => x.ProcessedAt != null);
            var pending = await db.OutboxMessages.CountAsync(x => x.ProcessedAt == null);
            Assert.Equal(100, processed);
            Assert.Equal(1, pending);
        }
    }
}
