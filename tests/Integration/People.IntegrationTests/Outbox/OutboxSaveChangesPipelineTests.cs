using Microsoft.EntityFrameworkCore;
using People.Domain.Entities;
using People.Domain.IntegrationEvents;
using People.Domain.ValueObjects;
using People.IntegrationTests.Infrastructure;
using AccountTestFactory = People.IntegrationTests.Infrastructure.AccountTestFactory;
using Xunit;

namespace People.IntegrationTests.Outbox;

[Collection(nameof(PostgresCollection))]
public sealed class OutboxPipelineTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task SaveNewAccount_PersistsOutboxMessage_InSameTransaction_WithoutMediatorPublishingAccountCreated()
    {
        await using var write = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);

        var account = Account.Create(
            "outbox-pipe",
            Language.Parse("en"),
            System.Net.IPAddress.Parse("198.51.100.50"),
            AccountTestFactory.FakeIpHasher(),
            TimeProvider.System
        );

        write.Accounts.Add(account);
        await write.SaveEntitiesAsync(CancellationToken.None);

        Assert.Equal(1, await write.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task SaveNewAccount_OutboxPayload_IsAccountCreatedIntegrationEvent_WithMatchingAccountIdAndIp()
    {
        await using var write = fixture.CreateContext();
        await IntegrationDatabaseCleanup.DeleteAllAsync(write);

        const string expectedIp = "198.51.100.51";
        var account = Account.Create(
            "outbox-map",
            Language.Parse("en"),
            System.Net.IPAddress.Parse(expectedIp),
            AccountTestFactory.FakeIpHasher(),
            TimeProvider.System
        );

        write.Accounts.Add(account);
        await write.SaveEntitiesAsync(CancellationToken.None);

        var row = await write.OutboxMessages.SingleAsync();
        var payload = row.GetPayload();
        var created = Assert.IsType<AccountCreatedIntegrationEvent>(payload);
        Assert.Equal((long)account.Id, created.AccountId);
        Assert.Equal(expectedIp, created.IpAddress);
    }
}
