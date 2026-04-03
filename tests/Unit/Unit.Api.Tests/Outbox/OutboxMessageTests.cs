using People.Infrastructure.Outbox.Entities;
using Xunit;

namespace Unit.Api.Tests.Outbox;

public sealed class OutboxMessageTests
{
    private static AccountCreatedIntegrationEvent CreatedPayload() =>
        new(Guid.CreateVersion7(), 42L, "203.0.113.1", new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Create_SetsStatusCreated()
    {
        var message = OutboxMessage.Create(CreatedPayload());
        Assert.Equal(OutboxStatus.Created, message.Status);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public void MarkProcessed_SetsSuccessAndProcessedAt()
    {
        var message = OutboxMessage.Create(CreatedPayload());
        var at = new DateTime(2026, 4, 2, 15, 30, 0, DateTimeKind.Utc);
        message.MarkProcessed(at);
        Assert.Equal(OutboxStatus.Completed, message.Status);
        Assert.Equal(at, message.ProcessedAt);
        Assert.Null(message.NextRetryAt);
    }

    [Fact]
    public void MarkFailed_IncrementsAttemptsAndSetsNextRetryAt_WhenBelowMax()
    {
        var message = OutboxMessage.Create(CreatedPayload());
        var failedAt = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);
        message.MarkFailed(failedAt, new Exception("temporary"));
        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.NotNull(message.NextRetryAt);
        Assert.True(message.NextRetryAt > failedAt);
    }

    [Fact]
    public void MarkFailed_After30Attempts_SetsFailAndProcessedAt()
    {
        var message = OutboxMessage.Create(CreatedPayload());
        var t = DateTime.UtcNow;
        for (var i = 0; i < OutboxMessage.MaxAttempts; i++)
            message.MarkFailed(t.AddSeconds(i), new Exception($"attempt {i}"));

        Assert.Equal(OutboxStatus.Failed, message.Status);
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.NextRetryAt);
    }

    [Fact]
    public void GetPayload_DeserializesCorrectIntegrationEventType()
    {
        var payload = CreatedPayload();
        var message = OutboxMessage.Create(payload);
        var roundTrip = message.GetPayload();
        var created = Assert.IsType<AccountCreatedIntegrationEvent>(roundTrip);
        Assert.Equal(payload.Id, created.Id);
        Assert.Equal(payload.AccountId, created.AccountId);
        Assert.Equal(payload.IpAddress, created.IpAddress);
        Assert.Equal(payload.OccurredAt, created.OccurredAt);
    }
}
