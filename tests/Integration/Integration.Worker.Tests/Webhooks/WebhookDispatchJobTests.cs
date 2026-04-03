using Integration.Shared.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using People.Application.Webhooks;
using People.Infrastructure;
using People.Infrastructure.Webhooks;
using People.Worker.Jobs;
using Quartz;
using Xunit;

namespace Integration.Worker.Tests.Webhooks;

[Collection(nameof(PostgresCollection))]
public sealed class WebhookDispatchJobTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Execute_WhenAllConsumersSucceed_DeletesMessage()
    {
        await using var db = fixture.CreateWebhookContext();
        await db.Messages.ExecuteDeleteAsync();
        await db.Consumers.ExecuteDeleteAsync();

        db.Consumers.Add(WebhookConsumer.Create(WebhookType.Created, WebhookMethod.Post, "https://hooks.example/c", null));
        db.Messages.Add(new WebhookMessage(1L, WebhookType.Created, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var sender = Substitute.For<IWebhookSender>();
        var job = CreateJob(fixture, sender);

        await job.Execute(CreateJobContext());

        await using var verifyDb = fixture.CreateWebhookContext();
        Assert.Equal(0, await verifyDb.Messages.CountAsync());
        await sender.Received(1).SendAsync(
            1L,
            Arg.Any<DateTime>(),
            Arg.Is<IEnumerable<WebhookConsumer>>(list => list.Count() == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenSendFails_IncrementsAttemptsAndSetsRetryAfter()
    {
        await using var db = fixture.CreateWebhookContext();
        await db.Messages.ExecuteDeleteAsync();
        await db.Consumers.ExecuteDeleteAsync();

        db.Consumers.Add(WebhookConsumer.Create(WebhookType.Created, WebhookMethod.Post, "https://hooks.example/c", null));
        db.Messages.Add(new WebhookMessage(2L, WebhookType.Created, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var sender = Substitute.For<IWebhookSender>();
        sender.SendAsync(
                Arg.Any<long>(),
                Arg.Any<DateTime>(),
                Arg.Any<IEnumerable<WebhookConsumer>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        var job = CreateJob(fixture, sender);

        await job.Execute(CreateJobContext());

        await using var verifyDb = fixture.CreateWebhookContext();
        var message = await verifyDb.Messages.SingleAsync();
        Assert.Equal(1, message.Attempts);
        Assert.Equal(WebhookStatus.Pending, message.Status);
        Assert.NotNull(message.RetryAfter);
    }

    [Fact]
    public async Task Execute_WhenNoMatchingConsumers_DeletesMessageWithoutSending()
    {
        await using var db = fixture.CreateWebhookContext();
        await db.Messages.ExecuteDeleteAsync();
        await db.Consumers.ExecuteDeleteAsync();

        db.Consumers.Add(WebhookConsumer.Create(WebhookType.Created, WebhookMethod.Post, "https://hooks.example/c", null));
        db.Messages.Add(new WebhookMessage(4L, WebhookType.Deleted, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var sender = Substitute.For<IWebhookSender>();
        var job = CreateJob(fixture, sender);

        await job.Execute(CreateJobContext());

        await using var verifyDb = fixture.CreateWebhookContext();
        Assert.Equal(0, await verifyDb.Messages.CountAsync());
        await sender.DidNotReceive()
            .SendAsync(
                Arg.Any<long>(),
                Arg.Any<DateTime>(),
                Arg.Any<IEnumerable<WebhookConsumer>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_MessageWithFutureRetryAfter_IsSkipped()
    {
        await using var db = fixture.CreateWebhookContext();
        await db.Messages.ExecuteDeleteAsync();
        await db.Consumers.ExecuteDeleteAsync();

        db.Consumers.Add(WebhookConsumer.Create(WebhookType.Created, WebhookMethod.Post, "https://hooks.example/c", null));
        db.Messages.Add(new WebhookMessage(5L, WebhookType.Created, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var sender = Substitute.For<IWebhookSender>();
        sender.SendAsync(
                Arg.Any<long>(),
                Arg.Any<DateTime>(),
                Arg.Any<IEnumerable<WebhookConsumer>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("error"));

        var job = CreateJob(fixture, sender);
        await job.Execute(CreateJobContext());

        sender.ClearReceivedCalls();
        sender.SendAsync(
                Arg.Any<long>(),
                Arg.Any<DateTime>(),
                Arg.Any<IEnumerable<WebhookConsumer>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await job.Execute(CreateJobContext());

        await using var verifyDb = fixture.CreateWebhookContext();
        Assert.Equal(1, await verifyDb.Messages.CountAsync());
        await sender.DidNotReceive()
            .SendAsync(
                Arg.Any<long>(),
                Arg.Any<DateTime>(),
                Arg.Any<IEnumerable<WebhookConsumer>>(),
                Arg.Any<CancellationToken>());
    }

    private static WebhookDispatchJob CreateJob(PostgreSqlFixture fixture, IWebhookSender sender)
    {
        var factory = new DelegatingWebhookDbContextFactory(fixture);

        return new WebhookDispatchJob(factory, sender, NullLogger<WebhookDispatchJob>.Instance);
    }

    private sealed class DelegatingWebhookDbContextFactory(PostgreSqlFixture fixture) : IDbContextFactory<WebhookDbContext>
    {
        public WebhookDbContext CreateDbContext() =>
            fixture.CreateWebhookContext();

        public Task<WebhookDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(fixture.CreateWebhookContext());
    }

    private static IJobExecutionContext CreateJobContext()
    {
        var context = Substitute.For<IJobExecutionContext>();
        context.FireTimeUtc.Returns(DateTimeOffset.UtcNow);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }
}
