using System.Net;
using System.Net.Mail;
using Mediator;
using NSubstitute;
using People.Api.Application.IntegrationEvents.Events;
using People.Api.Application.Queries.IsAccountActive;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.IntegrationTests.Commands;
using People.IntegrationTests.Infrastructure;
using Xunit;

namespace People.IntegrationTests.Queries;

public sealed class IsAccountActiveQueryTests(PostgreSqlFixture postgres) : QueryIntegrationTestBase(postgres)
{
    [Fact]
    public async Task ActiveAccount_ReturnsTrue_AndPublishesInspected()
    {
        Commands.EventBus.ClearReceivedCalls();

        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("active@example.com"),
                "active-user",
                CancellationToken.None);
        }

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var active = await sender.Send(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.True(active);
        await Commands.EventBus
            .Received(1)
            .PublishAsync(
                Arg.Is<AccountActivity.InspectedIntegrationEvent>(e => e.AccountId == (long)id),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InactiveAccount_ReturnsFalse_AndPublishesInspectedWhenRowExists()
    {
        Commands.EventBus.ClearReceivedCalls();

        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc));
        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create("inactive", Language.Parse("en"), IPAddress.Loopback, hasher, fixedTime);
            account.ClearDomainEvents();
            account.AddEmail(new MailAddress("pending@example.com"), false, fixedTime);

            await repo.AddAsync(account, CancellationToken.None);
            await repo.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
            id = account.Id;
        }

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var active = await sender.Send(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.False(active);
        await Commands.EventBus
            .Received(1)
            .PublishAsync(
                Arg.Is<AccountActivity.InspectedIntegrationEvent>(e => e.AccountId == (long)id),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BannedAccount_ReturnsFalse_AndPublishesInspectedWhenRowExists()
    {
        Commands.EventBus.ClearReceivedCalls();

        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc));
        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create("banned", Language.Parse("en"), IPAddress.Loopback, hasher, fixedTime);
            account.ClearDomainEvents();
            account.AddEmail(new MailAddress("banned@example.com"), true, fixedTime);
            account.Ban("terms", fixedTime.GetUtcNow().UtcDateTime.AddDays(30), fixedTime);

            await repo.AddAsync(account, CancellationToken.None);
            await repo.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
            id = account.Id;
        }

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var active = await sender.Send(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.False(active);
        await Commands.EventBus
            .Received(1)
            .PublishAsync(
                Arg.Is<AccountActivity.InspectedIntegrationEvent>(e => e.AccountId == (long)id),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownAccount_ReturnsFalse_WithoutPublishing()
    {
        Commands.EventBus.ClearReceivedCalls();

        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var active = await sender.Send(new IsAccountActiveQuery(new AccountId(77_777_777L)), CancellationToken.None);

        Assert.False(active);
        await Commands.EventBus
            .DidNotReceive()
            .PublishAsync(Arg.Any<AccountActivity.InspectedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
