using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using People.Application.Commands.EnrichAccount;
using People.Domain.IntegrationEvents;
using People.Infrastructure;
using People.Infrastructure.Outbox.Entities;
using Integration.Shared.Tests.Infrastructure;
using People.Application.Webhooks;
using People.Domain.ValueObjects;
using People.Worker.Commands;
using People.Worker.Jobs;
using Quartz;
using Xunit;

namespace Integration.Worker.Tests.Outbox;

[Collection(nameof(PostgresCollection))]
public sealed class OutboxDispatchJobTests(PostgreSqlFixture fixture)
{
    private static ServiceProvider CreateProvider(PostgreSqlFixture fx, ISender sender)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => sender);
        services.AddScoped<PeopleDbContext>(_ => fx.CreateContext());
        services.AddSingleton<IDbContextFactory<PeopleDbContext>>(
            new DelegatingDbContextFactory(fx));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private sealed class DelegatingDbContextFactory(PostgreSqlFixture fx) : IDbContextFactory<PeopleDbContext>
    {
        public PeopleDbContext CreateDbContext() =>
            fx.CreateContext();

        public Task<PeopleDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(fx.CreateContext());
    }

    private static OutboxDispatchJob CreateJob(ServiceProvider root) =>
        new(
            root.GetRequiredService<IServiceScopeFactory>(),
            root.GetRequiredService<IDbContextFactory<PeopleDbContext>>(),
            NullLogger<OutboxDispatchJob>.Instance);

    private static IJobExecutionContext FakeContext(CancellationToken ct = default)
    {
        var ctx = Substitute.For<IJobExecutionContext>();
        ctx.CancellationToken.Returns(ct);
        return ctx;
    }

    [Fact]
    public async Task Execute_DispatchesEnrichAndWebhooks_ForAccountCreatedPayload()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<EnrichAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(await ValueTask.FromResult(Unit.Value));
        mediator.Send(Arg.Any<CreateWebhookMessageCommand>(), Arg.Any<CancellationToken>())
            .Returns(await ValueTask.FromResult(Unit.Value));

        await using var root = CreateProvider(fixture, mediator);
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

        await using (var runRoot = CreateProvider(fixture, mediator))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await mediator.Received(1)
            .Send(
                Arg.Is<EnrichAccountCommand>(c => c.AccountId == 5001L && c.IpAddress == "198.51.100.10"),
                Arg.Any<CancellationToken>());
        await mediator.Received(1)
            .Send(
                Arg.Is<CreateWebhookMessageCommand>(c =>
                    c.AccountId == 5001L && c.Type == WebhookType.Created),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_DispatchesSendEmailVerification_ForEmailVerificationRequestedPayload()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<SendEmailVerificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(await ValueTask.FromResult(Unit.Value));

        await using var root = CreateProvider(fixture, mediator);
        await using (var seedScope = root.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await IntegrationDatabaseCleanup.DeleteAllAsync(db);
            var evt = new EmailVerificationRequestedIntegrationEvent(
                Guid.CreateVersion7(),
                5004L,
                "verify@example.com",
                Language.Default,
                DateTime.UtcNow.AddMinutes(-10));
            db.OutboxMessages.Add(OutboxMessage.Create(evt));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var runRoot = CreateProvider(fixture, mediator))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await mediator.Received(1)
            .Send(
                Arg.Is<SendEmailVerificationCommand>(c =>
                    c.AccountId == 5004L &&
                    c.Email == "verify@example.com" &&
                    c.Language == Language.Default
                ),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_OnHandlerFailure_SchedulesRetry_WithPendingStatus()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<EnrichAccountCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("handler down"));

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
        sender.Send(Arg.Any<CreateWebhookMessageCommand>(), Arg.Any<CancellationToken>())
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

        await sender.Received(1)
            .Send(
                Arg.Is<CreateWebhookMessageCommand>(c => c.AccountId == 5003L && c.Type == WebhookType.Updated),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ProcessesAtMost100Messages_PerRun()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<EnrichAccountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Unit.Value));
        sender.Send(Arg.Any<CreateWebhookMessageCommand>(), Arg.Any<CancellationToken>())
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
