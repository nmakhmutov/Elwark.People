using NSubstitute;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.IntegrationEvents.EventHandling;
using People.Domain.Entities;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.EventHandlers;

public sealed class AccountEngagedIntegrationEventHandlerTests(PostgreSqlFixture postgres)
    : IntegrationEventHandlerIntegrationTestBase(postgres)
{
    [Fact]
    public async Task HandleAsync_LoggedIn_UpdatesLastLogIn_AndDeletesConfirmation()
    {
        Fx.ResetExternalMocks();

        using (var resetScope = Fx.CreateScope())
        {
            var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await CommandTestFixture.ResetDatabaseAsync(resetDb);
        }

        AccountId accountId;
        using (var seedScope = Fx.CreateScope())
        {
            accountId = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new System.Net.Mail.MailAddress("engaged-login@example.com")
            );
        }

        var message = new AccountActivity.LoggedInIntegrationEvent(accountId);

        using (var runScope = Fx.CreateScope())
        {
            var handler = runScope.ServiceProvider.GetRequiredService<AccountEngagedIntegrationEventHandler>();
            await handler.HandleAsync(message, CancellationToken.None);
        }

        using (var readScope = Fx.CreateScope())
        {
            var db = readScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            var lastLogIn = await IntegrationEventHandlerTestFixture.QueryAccountTimestampUtcAsync(
                db,
                accountId,
                "last_log_in",
                CancellationToken.None
            );
            Assert.Equal(message.CreatedAt, lastLogIn);
        }

        await Fx.Confirmation.Received(1).DeleteAsync(accountId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Inspected_UpdatesUpdatedAt_AndDeletesConfirmation()
    {
        Fx.ResetExternalMocks();

        using (var resetScope = Fx.CreateScope())
        {
            var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            await CommandTestFixture.ResetDatabaseAsync(resetDb);
        }

        AccountId accountId;
        using (var seedScope = Fx.CreateScope())
        {
            accountId = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new System.Net.Mail.MailAddress("engaged-inspect@example.com")
            );
        }

        var message = new AccountActivity.InspectedIntegrationEvent(accountId);

        using (var runScope = Fx.CreateScope())
        {
            var handler = runScope.ServiceProvider.GetRequiredService<AccountEngagedIntegrationEventHandler>();
            await handler.HandleAsync(message, CancellationToken.None);
        }

        using (var readScope = Fx.CreateScope())
        {
            var db = readScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            var updatedAt = await IntegrationEventHandlerTestFixture.QueryAccountTimestampUtcAsync(
                db,
                accountId,
                "updated_at",
                CancellationToken.None
            );
            Assert.Equal(message.CreatedAt, updatedAt);
        }

        await Fx.Confirmation.Received(1).DeleteAsync(accountId, Arg.Any<CancellationToken>());
    }
}
