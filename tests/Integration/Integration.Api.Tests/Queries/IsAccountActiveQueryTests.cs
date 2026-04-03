using System.Net;
using System.Net.Mail;
using Mediator;
using People.Application.Queries.IsAccountActive;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using People.Infrastructure;
using Integration.Api.Tests.Commands;
using Xunit;

namespace Integration.Api.Tests.Queries;

public sealed class IsAccountActiveQueryTests(PostgreSqlFixture postgres) : QueryIntegrationTestBase(postgres)
{
    [Fact]
    public async Task ActiveAccount_ReturnsTrue()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            id = await CommandTestFixture.SeedAccountWithConfirmedEmailAsync(
                seedScope,
                new MailAddress("active@example.com"),
                CancellationToken.None
            );
        }

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var active = await sender.Send(new IsAccountActiveQuery(id), CancellationToken.None);

        Assert.True(active);
    }

    [Fact]
    public async Task InactiveAccount_ReturnsFalse()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc));
        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create(Language.Parse("en"), IPAddress.Loopback, hasher, fixedTime);
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
    }

    [Fact]
    public async Task BannedAccount_ReturnsFalse()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc));
        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create(Language.Parse("en"), IPAddress.Loopback, hasher, fixedTime);
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
    }

    [Fact]
    public async Task UnknownAccount_ReturnsFalse()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var active = await sender.Send(new IsAccountActiveQuery(new AccountId(77_777_777L)), CancellationToken.None);

        Assert.False(active);
    }
}
