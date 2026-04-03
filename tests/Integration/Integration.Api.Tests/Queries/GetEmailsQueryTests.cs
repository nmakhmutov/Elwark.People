using System.Net;
using System.Net.Mail;
using Mediator;
using People.Application.Queries.GetEmails;
using People.Domain.Entities;
using People.Domain.Repositories;
using People.Domain.SeedWork;
using People.Domain.ValueObjects;
using Integration.Api.Tests.Commands;
using Integration.Shared.Tests.Infrastructure;
using People.Infrastructure;
using Xunit;

namespace Integration.Api.Tests.Queries;

public sealed class GetEmailsQueryTests(PostgreSqlFixture postgres) : QueryIntegrationTestBase(postgres)
{
    [Fact]
    public async Task Handle_ReturnsAllEmailsWithPrimaryAndConfirmedFlags()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        var primary = new MailAddress("emails-primary@example.com");
        var secondaryConfirmed = new MailAddress("emails-sec-ok@example.com");
        var secondaryPending = new MailAddress("emails-sec-pend@example.com");
        var fixedTime = AccountTestFactory.FixedUtc(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));

        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create(Language.Parse("en"), IPAddress.Loopback, hasher, fixedTime);
            account.ClearDomainEvents();
            account.AddEmail(primary, true, fixedTime);
            account.AddEmail(secondaryConfirmed, true, fixedTime);
            account.AddEmail(secondaryPending, false, fixedTime);

            await repo.AddAsync(account, CancellationToken.None);
            await repo.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
            id = account.Id;
        }

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetEmailsQuery(id), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Contains(
            result,
            e => e is { Email: "emails-primary@example.com", IsPrimary: true, IsConfirmed: true });
        Assert.Contains(
            result,
            e => e is { Email: "emails-sec-ok@example.com", IsPrimary: false, IsConfirmed: true });
        Assert.Contains(
            result,
            e => e is { Email: "emails-sec-pend@example.com", IsPrimary: false, IsConfirmed: false });
    }

    [Fact]
    public async Task Handle_NoEmails_ReturnsEmpty()
    {
        using var resetScope = Commands.CreateScope();
        await using var resetDb = resetScope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await CommandTestFixture.ResetDatabaseAsync(resetDb);

        AccountId id;
        using (var seedScope = Commands.CreateScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var hasher = seedScope.ServiceProvider.GetRequiredService<IIpHasher>();

            var account = Account.Create(Language.Parse("en"), IPAddress.Loopback, hasher, TimeProvider.System);
            account.ClearDomainEvents();

            await repo.AddAsync(account, CancellationToken.None);
            await repo.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
            id = account.Id;
        }

        using var scope = Commands.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetEmailsQuery(id), CancellationToken.None);

        Assert.Empty(result);
    }
}
