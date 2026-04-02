using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using People.Domain.IntegrationEvents;
using People.Infrastructure;
using People.Infrastructure.Outbox;
using People.IntegrationTests.Infrastructure;
using People.Worker.Jobs;
using Quartz;
using Xunit;

namespace People.IntegrationTests.Outbox;

[Collection(nameof(PostgresCollection))]
public sealed class OutboxCleanupJobTests(PostgreSqlFixture fixture)
{
    private static ServiceProvider CreateProvider(PostgreSqlFixture fx)
    {
        var services = new ServiceCollection();
        services.AddScoped<PeopleDbContext>(_ => fx.CreateContext(new NoOpMediator()));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static OutboxCleanupJob CreateJob(ServiceProvider root) =>
        new(root.GetRequiredService<IServiceScopeFactory>());

    private static IJobExecutionContext FakeContext(CancellationToken ct = default)
    {
        var ctx = Substitute.For<IJobExecutionContext>();
        ctx.CancellationToken.Returns(ct);
        return ctx;
    }

    [Fact]
    public async Task Execute_DeletesSuccessMessagesOlderThanSevenDays()
    {
        await using var root = CreateProvider(fixture);
        await using (var scope = root.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await IntegrationDatabaseCleanup.DeleteAllAsync(db);
            var old = OutboxMessage.Create(
                new AccountUpdatedIntegrationEvent(
                    Guid.CreateVersion7(),
                    1L,
                    DateTime.UtcNow));
            old.MarkProcessed(DateTime.UtcNow.AddDays(-8));
            db.OutboxMessages.Add(old);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var runRoot = CreateProvider(fixture))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await using (var verifyRoot = CreateProvider(fixture))
        {
            await using var scope = verifyRoot.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            Assert.Equal(0, await db.OutboxMessages.CountAsync());
        }
    }

    [Fact]
    public async Task Execute_DeletesFailMessagesOlderThanThirtyDays()
    {
        await using var root = CreateProvider(fixture);
        await using (var scope = root.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await IntegrationDatabaseCleanup.DeleteAllAsync(db);
            var msg = OutboxMessage.Create(
                new AccountDeletedIntegrationEvent(
                    Guid.CreateVersion7(),
                    2L,
                    DateTime.UtcNow));
            var failAt = DateTime.UtcNow.AddDays(-31);
            for (var i = 0; i < OutboxMessage.MaxAttempts; i++)
                msg.MarkFailed(failAt.AddSeconds(i), new Exception($"e{i}"));

            db.OutboxMessages.Add(msg);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var runRoot = CreateProvider(fixture))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await using (var verifyRoot = CreateProvider(fixture))
        {
            await using var scope = verifyRoot.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            Assert.Equal(0, await db.OutboxMessages.CountAsync());
        }
    }

    [Fact]
    public async Task Execute_DoesNotDeletePendingMessages()
    {
        await using var root = CreateProvider(fixture);
        await using (var scope = root.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await IntegrationDatabaseCleanup.DeleteAllAsync(db);
            db.OutboxMessages.Add(
                OutboxMessage.Create(
                    new AccountCreatedIntegrationEvent(
                        Guid.CreateVersion7(),
                        3L,
                        "1.1.1.1",
                        DateTime.UtcNow.AddDays(-40))));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var runRoot = CreateProvider(fixture))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await using (var verifyRoot = CreateProvider(fixture))
        {
            await using var scope = verifyRoot.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            Assert.Equal(1, await db.OutboxMessages.CountAsync());
        }
    }

    [Fact]
    public async Task Execute_DoesNotDeleteRecentSuccessMessages()
    {
        await using var root = CreateProvider(fixture);
        await using (var scope = root.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await IntegrationDatabaseCleanup.DeleteAllAsync(db);
            var msg = OutboxMessage.Create(
                new AccountUpdatedIntegrationEvent(
                    Guid.CreateVersion7(),
                    4L,
                    DateTime.UtcNow));
            msg.MarkProcessed(DateTime.UtcNow.AddDays(-1));
            db.OutboxMessages.Add(msg);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await using (var runRoot = CreateProvider(fixture))
        {
            var job = CreateJob(runRoot);
            await job.Execute(FakeContext());
        }

        await using (var verifyRoot = CreateProvider(fixture))
        {
            await using var scope = verifyRoot.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            Assert.Equal(1, await db.OutboxMessages.CountAsync());
        }
    }
}
