using System.Net.Mail;
using Mediator;
using People.Api.Application.Commands.UpdateLastActivity;
using People.Domain.Entities;
using People.Infrastructure;
using People.IntegrationTests.EventHandlers;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Commands;

[Collection(nameof(PostgresCollection))]
public sealed class UpdateLastActivityCommandTests(PostgreSqlFixture postgres) : CommandIntegrationTestBase(postgres)
{
    [Fact]
    public async Task Handle_UpdatesUpdatedAt()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId accountId;
        using (var seedScope = Commands.CreateScope())
        {
            accountId = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("activity@example.com"),
                "activity-cmd-test",
                CancellationToken.None);
        }

        var before = DateTime.UtcNow;

        using (var runScope = Commands.CreateScope())
        {
            var sender = runScope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new UpdateLastActivityCommand(accountId), CancellationToken.None);
        }

        await using var readDb = Commands.CreateReadOnlyContext();
        var updatedAt = await IntegrationEventHandlerTestFixture.QueryAccountTimestampUtcAsync(
            readDb,
            (long)accountId,
            "updated_at",
            CancellationToken.None);

        Assert.InRange(updatedAt, before.AddSeconds(-10), DateTime.UtcNow.AddSeconds(10));
    }
}
