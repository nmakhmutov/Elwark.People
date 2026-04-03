using People.Application.Webhooks;
using People.Infrastructure.Webhooks;
using Xunit;

namespace Unit.Api.Tests.Application.Providers.Webhooks;

public sealed class WebhookMessageTests
{
    [Fact]
    public void MarkFailed_FirstFailure_IncrementsAttemptsAndSetsRetryAfter()
    {
        var message = new WebhookMessage(1L, WebhookType.Created, DateTime.UtcNow);
        var retryAfter = DateTime.UtcNow.AddMinutes(1);

        message.MarkFailed(retryAfter);

        Assert.Equal(1, message.Attempts);
        Assert.Equal(WebhookStatus.Pending, message.Status);
        Assert.Equal(retryAfter, message.RetryAfter);
    }

    [Fact]
    public void MarkFailed_NinthFailure_StillPending()
    {
        var message = new WebhookMessage(1L, WebhookType.Created, DateTime.UtcNow);
        var retryAfter = DateTime.UtcNow.AddMinutes(1);

        for (var i = 0; i < 9; i++)
            message.MarkFailed(retryAfter);

        Assert.Equal(9, message.Attempts);
        Assert.Equal(WebhookStatus.Pending, message.Status);
    }

    [Fact]
    public void MarkFailed_TenthFailure_SetsFailedStatus()
    {
        var message = new WebhookMessage(1L, WebhookType.Created, DateTime.UtcNow);
        var retryAfter = DateTime.UtcNow.AddMinutes(1);

        for (var i = 0; i < 10; i++)
            message.MarkFailed(retryAfter);

        Assert.Equal(10, message.Attempts);
        Assert.Equal(WebhookStatus.Failed, message.Status);
    }
}
