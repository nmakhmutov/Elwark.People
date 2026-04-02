using System.Net;
using System.Net.Mail;
using Mediator;
using People.Api.Application.Commands.UpdateLastLogin;
using People.Domain.Entities;
using People.Infrastructure;
using People.IntegrationTests.EventHandlers;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Commands;

[Collection(nameof(PostgresCollection))]
public sealed class UpdateLastLoginCommandTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task Handle_UpdatesLastLoginAndUpdatedAt()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId accountId;
        using (var seedScope = Commands.CreateScope())
        {
            accountId = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("lastlogin@example.com"),
                "login-cmd-test",
                CancellationToken.None);
        }

        var before = DateTime.UtcNow;

        using (var runScope = Commands.CreateScope())
        {
            var sender = runScope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new UpdateLastLoginCommand(accountId), CancellationToken.None);
        }

        await using var readDb = Commands.CreateReadOnlyContext();
        var lastLogIn = await IntegrationEventHandlerTestFixture.QueryAccountTimestampUtcAsync(
            readDb,
            (long)accountId,
            "last_log_in",
            CancellationToken.None);
        var updatedAt = await IntegrationEventHandlerTestFixture.QueryAccountTimestampUtcAsync(
            readDb,
            (long)accountId,
            "updated_at",
            CancellationToken.None);

        Assert.InRange(lastLogIn, before.AddSeconds(-10), DateTime.UtcNow.AddSeconds(10));
        Assert.InRange(updatedAt, before.AddSeconds(-10), DateTime.UtcNow.AddSeconds(10));
    }
}
